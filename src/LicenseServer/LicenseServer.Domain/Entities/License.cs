using LicenseServer.Domain.Enums;

namespace LicenseServer.Domain.Entities;

public class License
{
    public Guid Id { get; private set; }
    public string LicenseKey { get; private set; } = string.Empty;
    public string SecretKeyHash { get; private set; } = string.Empty;
    public Guid TenantId { get; private set; }
    public ClientTenant Tenant { get; private set; } = null!;

    public LicenseType Type { get; private set; }
    public LicenseStatus Status { get; private set; }
    
    public int? MaxOrders { get; private set; }
    public int UsedOrders { get; private set; }
    public DateTimeOffset? ExpirationDate { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public DateTimeOffset? LastVerifiedAt { get; private set; }

    public List<LicenseUsageLog> UsageLogs { get; private set; } = [];

    private License() { }

    public static License CreateOrderLimitLicense(Guid tenantId, int maxOrders, string? customKey = null)
    {
        return new License
        {
            Id = Guid.NewGuid(),
            LicenseKey = customKey ?? $"LIC-ORD-{Guid.NewGuid():N}"[..24].ToUpperInvariant(),
            SecretKeyHash = Guid.NewGuid().ToString("N"),
            TenantId = tenantId,
            Type = LicenseType.OrderLimit,
            Status = LicenseStatus.Active,
            MaxOrders = maxOrders,
            UsedOrders = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static License CreateTimeLimitLicense(Guid tenantId, DateTimeOffset expirationDate, string? customKey = null)
    {
        return new License
        {
            Id = Guid.NewGuid(),
            LicenseKey = customKey ?? $"LIC-TIM-{Guid.NewGuid():N}"[..24].ToUpperInvariant(),
            SecretKeyHash = Guid.NewGuid().ToString("N"),
            TenantId = tenantId,
            Type = LicenseType.TimeLimit,
            Status = LicenseStatus.Active,
            ExpirationDate = expirationDate,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static License CreateLifetimeLicense(Guid tenantId, string? customKey = null)
    {
        return new License
        {
            Id = Guid.NewGuid(),
            LicenseKey = customKey ?? $"LIC-LFT-{Guid.NewGuid():N}"[..24].ToUpperInvariant(),
            SecretKeyHash = Guid.NewGuid().ToString("N"),
            TenantId = tenantId,
            Type = LicenseType.Lifetime,
            Status = LicenseStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public bool IsValid()
    {
        if (Status != LicenseStatus.Active)
            return false;

        if (Type == LicenseType.TimeLimit && ExpirationDate.HasValue && ExpirationDate.Value < DateTimeOffset.UtcNow)
        {
            Status = LicenseStatus.Expired;
            return false;
        }

        if (Type == LicenseType.OrderLimit && MaxOrders.HasValue && UsedOrders >= MaxOrders.Value)
        {
            return false;
        }

        return true;
    }

    public bool CanProcessOrder()
    {
        if (!IsValid())
            return false;

        if (Type == LicenseType.OrderLimit && MaxOrders.HasValue && UsedOrders >= MaxOrders.Value)
            return false;

        return true;
    }

    public bool RecordOrder()
    {
        if (!CanProcessOrder())
            return false;

        UsedOrders++;
        UpdatedAt = DateTimeOffset.UtcNow;
        return true;
    }

    public void UpgradeToLifetime()
    {
        Type = LicenseType.Lifetime;
        Status = LicenseStatus.Active;
        MaxOrders = null;
        ExpirationDate = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetOrderLimit(int newLimit)
    {
        Type = LicenseType.OrderLimit;
        MaxOrders = newLimit;
        Status = LicenseStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ExtendExpiration(DateTimeOffset newExpiration)
    {
        Type = LicenseType.TimeLimit;
        ExpirationDate = newExpiration;
        Status = LicenseStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Suspend()
    {
        Status = LicenseStatus.Suspended;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        Status = LicenseStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Revoke()
    {
        Status = LicenseStatus.Revoked;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void TouchVerification()
    {
        LastVerifiedAt = DateTimeOffset.UtcNow;
    }
}
