using Shop.Domain.Common;

namespace Shop.Domain.Entities;

public class AuditLog : Entity<Guid>
{
    public string AdminId { get; private set; } = string.Empty;
    public string AdminName { get; private set; } = string.Empty;
    public string AdminRole { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string Entity { get; private set; } = string.Empty;
    public string BeforeValue { get; private set; } = string.Empty;
    public string AfterValue { get; private set; } = string.Empty;
    public string IpAddress { get; private set; } = string.Empty;
    public string Timestamp { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private AuditLog() { }

    public AuditLog(
        string adminId,
        string adminName,
        string adminRole,
        string action,
        string entity,
        string beforeValue,
        string afterValue,
        string ipAddress,
        string? timestamp = null,
        Guid? id = null) : base(id ?? Guid.NewGuid())
    {
        AdminId = adminId;
        AdminName = adminName;
        AdminRole = adminRole;
        Action = action;
        Entity = entity;
        BeforeValue = beforeValue;
        AfterValue = afterValue;
        IpAddress = ipAddress;
        Timestamp = timestamp ?? DateTime.Now.ToString("yyyy/MM/dd - HH:mm");
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
