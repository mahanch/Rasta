using LicenseServer.Domain.Entities;
using LicenseServer.Domain.Enums;
using LicenseServer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LicenseServer.Infrastructure.Services;

public record IssueLicenseRequest(
    string StoreName,
    string OwnerName,
    string ContactEmail,
    string? PhoneNumber,
    string? DomainOrHost,
    LicenseType Type,
    int? MaxOrders,
    DateTimeOffset? ExpirationDate,
    string? CustomLicenseKey
);

public record LicenseResponseDto(
    Guid Id,
    string LicenseKey,
    string StoreName,
    string OwnerName,
    string ContactEmail,
    LicenseType Type,
    LicenseStatus Status,
    int? MaxOrders,
    int UsedOrders,
    int? RemainingOrders,
    DateTimeOffset? ExpirationDate,
    bool IsValid,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastVerifiedAt
);

public record VerifyLicenseResult(
    bool IsValid,
    string LicenseKey,
    string Status,
    string Type,
    int? MaxOrders,
    int UsedOrders,
    int? RemainingOrders,
    DateTimeOffset? ExpirationDate,
    string Message,
    SignedLicensePayload? SignedPayload
);

public record RecordOrderUsageResult(
    bool Success,
    string LicenseKey,
    int UsedOrders,
    int? MaxOrders,
    int? RemainingOrders,
    string Message
);

public interface ILicenseManager
{
    Task<LicenseResponseDto> IssueLicenseAsync(IssueLicenseRequest request, CancellationToken cancellationToken = default);
    Task<List<LicenseResponseDto>> GetAllLicensesAsync(LicenseStatus? statusFilter = null, CancellationToken cancellationToken = default);
    Task<LicenseResponseDto?> GetLicenseByKeyAsync(string licenseKey, CancellationToken cancellationToken = default);
    Task<VerifyLicenseResult> VerifyLicenseAsync(string licenseKey, string? clientIp = null, CancellationToken cancellationToken = default);
    Task<RecordOrderUsageResult> RecordOrderUsageAsync(string licenseKey, string? clientIp = null, CancellationToken cancellationToken = default);
    Task<LicenseResponseDto?> UpgradeLicenseAsync(string licenseKey, LicenseType newType, int? maxOrders = null, DateTimeOffset? expirationDate = null, CancellationToken cancellationToken = default);
    Task<bool> UpdateStatusAsync(string licenseKey, LicenseStatus status, CancellationToken cancellationToken = default);
    Task<List<LicenseUsageLog>> GetUsageLogsAsync(string licenseKey, CancellationToken cancellationToken = default);
}

public class LicenseManager : ILicenseManager
{
    private readonly LicenseDbContext _db;
    private readonly ILicenseCryptoService _crypto;

    public LicenseManager(LicenseDbContext db, ILicenseCryptoService crypto)
    {
        _db = db;
        _crypto = crypto;
    }

    public async Task<LicenseResponseDto> IssueLicenseAsync(IssueLicenseRequest request, CancellationToken cancellationToken = default)
    {
        var tenant = new ClientTenant(request.StoreName, request.OwnerName, request.ContactEmail, request.PhoneNumber, request.DomainOrHost);
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(cancellationToken);

        License license;
        switch (request.Type)
        {
            case LicenseType.OrderLimit:
                license = License.CreateOrderLimitLicense(tenant.Id, request.MaxOrders ?? 100, request.CustomLicenseKey);
                break;
            case LicenseType.TimeLimit:
                var exp = request.ExpirationDate ?? DateTimeOffset.UtcNow.AddDays(30);
                license = License.CreateTimeLimitLicense(tenant.Id, exp, request.CustomLicenseKey);
                break;
            case LicenseType.Lifetime:
            default:
                license = License.CreateLifetimeLicense(tenant.Id, request.CustomLicenseKey);
                break;
        }

        _db.Licenses.Add(license);
        _db.UsageLogs.Add(new LicenseUsageLog(license.Id, "Issued", $"License issued with type: {license.Type}"));
        await _db.SaveChangesAsync(cancellationToken);

        return MapToDto(license, tenant);
    }

    public async Task<List<LicenseResponseDto>> GetAllLicensesAsync(LicenseStatus? statusFilter = null, CancellationToken cancellationToken = default)
    {
        var query = _db.Licenses.Include(l => l.Tenant).AsNoTracking();
        if (statusFilter.HasValue)
        {
            query = query.Where(l => l.Status == statusFilter.Value);
        }

        var list = await query.OrderByDescending(l => l.CreatedAt).ToListAsync(cancellationToken);
        return list.Select(l => MapToDto(l, l.Tenant)).ToList();
    }

    public async Task<LicenseResponseDto?> GetLicenseByKeyAsync(string licenseKey, CancellationToken cancellationToken = default)
    {
        var license = await _db.Licenses.Include(l => l.Tenant)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.LicenseKey == licenseKey, cancellationToken);

        return license == null ? null : MapToDto(license, license.Tenant);
    }

    public async Task<VerifyLicenseResult> VerifyLicenseAsync(string licenseKey, string? clientIp = null, CancellationToken cancellationToken = default)
    {
        var license = await _db.Licenses.FirstOrDefaultAsync(l => l.LicenseKey == licenseKey, cancellationToken);
        if (license == null)
        {
            return new VerifyLicenseResult(false, licenseKey, "NotFound", "Unknown", null, 0, null, null, "License key not found.", null);
        }

        license.TouchVerification();
        var isValid = license.IsValid();

        _db.UsageLogs.Add(new LicenseUsageLog(license.Id, "Verify", $"Verification called. Valid: {isValid}", clientIp));
        await _db.SaveChangesAsync(cancellationToken);

        var signedPayload = _crypto.SignLicense(
            license.LicenseKey,
            license.Status.ToString(),
            license.Type.ToString(),
            license.MaxOrders,
            license.UsedOrders,
            license.ExpirationDate,
            isValid
        );

        int? remainingOrders = license.MaxOrders.HasValue ? Math.Max(0, license.MaxOrders.Value - license.UsedOrders) : null;
        string msg = isValid ? "License is valid." : $"License is invalid or exceeded: {license.Status}";

        return new VerifyLicenseResult(
            isValid,
            license.LicenseKey,
            license.Status.ToString(),
            license.Type.ToString(),
            license.MaxOrders,
            license.UsedOrders,
            remainingOrders,
            license.ExpirationDate,
            msg,
            signedPayload
        );
    }

    public async Task<RecordOrderUsageResult> RecordOrderUsageAsync(string licenseKey, string? clientIp = null, CancellationToken cancellationToken = default)
    {
        var license = await _db.Licenses.FirstOrDefaultAsync(l => l.LicenseKey == licenseKey, cancellationToken);
        if (license == null)
        {
            return new RecordOrderUsageResult(false, licenseKey, 0, null, null, "License key not found.");
        }

        if (!license.RecordOrder())
        {
            _db.UsageLogs.Add(new LicenseUsageLog(license.Id, "RecordOrder_Blocked", $"Order creation blocked. Max: {license.MaxOrders}, Used: {license.UsedOrders}", clientIp));
            await _db.SaveChangesAsync(cancellationToken);
            return new RecordOrderUsageResult(false, license.LicenseKey, license.UsedOrders, license.MaxOrders, 0, "License limit exceeded or license inactive.");
        }

        _db.UsageLogs.Add(new LicenseUsageLog(license.Id, "RecordOrder_Success", $"Order recorded. New Used count: {license.UsedOrders}", clientIp));
        await _db.SaveChangesAsync(cancellationToken);

        int? remaining = license.MaxOrders.HasValue ? Math.Max(0, license.MaxOrders.Value - license.UsedOrders) : null;
        return new RecordOrderUsageResult(true, license.LicenseKey, license.UsedOrders, license.MaxOrders, remaining, "Order usage recorded successfully.");
    }

    public async Task<LicenseResponseDto?> UpgradeLicenseAsync(string licenseKey, LicenseType newType, int? maxOrders = null, DateTimeOffset? expirationDate = null, CancellationToken cancellationToken = default)
    {
        var license = await _db.Licenses.Include(l => l.Tenant).FirstOrDefaultAsync(l => l.LicenseKey == licenseKey, cancellationToken);
        if (license == null) return null;

        switch (newType)
        {
            case LicenseType.Lifetime:
                license.UpgradeToLifetime();
                break;
            case LicenseType.OrderLimit:
                license.SetOrderLimit(maxOrders ?? (license.UsedOrders + 100));
                break;
            case LicenseType.TimeLimit:
                license.ExtendExpiration(expirationDate ?? DateTimeOffset.UtcNow.AddMonths(1));
                break;
        }

        _db.UsageLogs.Add(new LicenseUsageLog(license.Id, "Upgrade", $"License upgraded to {newType}."));
        await _db.SaveChangesAsync(cancellationToken);

        return MapToDto(license, license.Tenant);
    }

    public async Task<bool> UpdateStatusAsync(string licenseKey, LicenseStatus status, CancellationToken cancellationToken = default)
    {
        var license = await _db.Licenses.FirstOrDefaultAsync(l => l.LicenseKey == licenseKey, cancellationToken);
        if (license == null) return false;

        switch (status)
        {
            case LicenseStatus.Active: license.Activate(); break;
            case LicenseStatus.Suspended: license.Suspend(); break;
            case LicenseStatus.Revoked: license.Revoke(); break;
        }

        _db.UsageLogs.Add(new LicenseUsageLog(license.Id, "StatusChange", $"Status changed to {status}."));
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<List<LicenseUsageLog>> GetUsageLogsAsync(string licenseKey, CancellationToken cancellationToken = default)
    {
        var license = await _db.Licenses.AsNoTracking().FirstOrDefaultAsync(l => l.LicenseKey == licenseKey, cancellationToken);
        if (license == null) return [];

        return await _db.UsageLogs.AsNoTracking()
            .Where(u => u.LicenseId == license.Id)
            .OrderByDescending(u => u.Timestamp)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    private static LicenseResponseDto MapToDto(License l, ClientTenant t)
    {
        int? remaining = l.MaxOrders.HasValue ? Math.Max(0, l.MaxOrders.Value - l.UsedOrders) : null;
        return new LicenseResponseDto(
            l.Id,
            l.LicenseKey,
            t.StoreName,
            t.OwnerName,
            t.ContactEmail,
            l.Type,
            l.Status,
            l.MaxOrders,
            l.UsedOrders,
            remaining,
            l.ExpirationDate,
            l.IsValid(),
            l.CreatedAt,
            l.LastVerifiedAt
        );
    }
}
