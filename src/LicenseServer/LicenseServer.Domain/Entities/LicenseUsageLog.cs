namespace LicenseServer.Domain.Entities;

public class LicenseUsageLog
{
    public Guid Id { get; private set; }
    public Guid LicenseId { get; private set; }
    public License License { get; private set; } = null!;
    public string Action { get; private set; } = string.Empty; // e.g. "Verify", "RecordOrder", "Heartbeat", "Upgrade", "Suspend"
    public string Details { get; private set; } = string.Empty;
    public string? ClientIp { get; private set; }
    public DateTimeOffset Timestamp { get; private set; } = DateTimeOffset.UtcNow;

    private LicenseUsageLog() { }

    public LicenseUsageLog(Guid licenseId, string action, string details, string? clientIp = null)
    {
        Id = Guid.NewGuid();
        LicenseId = licenseId;
        Action = action;
        Details = details;
        ClientIp = clientIp;
        Timestamp = DateTimeOffset.UtcNow;
    }
}
