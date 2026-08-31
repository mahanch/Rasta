using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LicenseServer.Infrastructure.Services;

public record SignedLicensePayload(
    string LicenseKey,
    string Status,
    string Type,
    int? MaxOrders,
    int UsedOrders,
    DateTimeOffset? ExpirationDate,
    bool IsValid,
    DateTimeOffset IssuedAt,
    string Signature
);

public interface ILicenseCryptoService
{
    SignedLicensePayload SignLicense(
        string licenseKey,
        string status,
        string type,
        int? maxOrders,
        int usedOrders,
        DateTimeOffset? expirationDate,
        bool isValid);

    bool VerifySignature(SignedLicensePayload payload);
}

public class LicenseCryptoService : ILicenseCryptoService
{
    private readonly byte[] _secretKey;

    public LicenseCryptoService(string? secret = null)
    {
        var secretString = secret ?? "ENTERPRISE_LICENSE_MASTER_SECRET_KEY_2026_VERY_SECURE_KEY!";
        _secretKey = Encoding.UTF8.GetBytes(secretString);
    }

    public SignedLicensePayload SignLicense(
        string licenseKey,
        string status,
        string type,
        int? maxOrders,
        int usedOrders,
        DateTimeOffset? expirationDate,
        bool isValid)
    {
        var issuedAt = DateTimeOffset.UtcNow;
        var dataToSign = $"{licenseKey}|{status}|{type}|{maxOrders}|{usedOrders}|{expirationDate?.ToUnixTimeSeconds()}|{isValid}|{issuedAt.ToUnixTimeSeconds()}";
        
        using var hmac = new HMACSHA256(_secretKey);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToSign));
        var signature = Convert.ToBase64String(hash);

        return new SignedLicensePayload(
            licenseKey,
            status,
            type,
            maxOrders,
            usedOrders,
            expirationDate,
            isValid,
            issuedAt,
            signature
        );
    }

    public bool VerifySignature(SignedLicensePayload payload)
    {
        var dataToSign = $"{payload.LicenseKey}|{payload.Status}|{payload.Type}|{payload.MaxOrders}|{payload.UsedOrders}|{payload.ExpirationDate?.ToUnixTimeSeconds()}|{payload.IsValid}|{payload.IssuedAt.ToUnixTimeSeconds()}";
        
        using var hmac = new HMACSHA256(_secretKey);
        var expectedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToSign));
        var expectedSignature = Convert.ToBase64String(expectedHash);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSignature),
            Encoding.UTF8.GetBytes(payload.Signature)
        );
    }
}
