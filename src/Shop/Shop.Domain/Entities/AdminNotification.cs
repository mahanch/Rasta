using Shop.Domain.Common;

namespace Shop.Domain.Entities;

public class AdminNotification : Entity<Guid>
{
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string Type { get; private set; } = "info"; // "info", "warning", "success", "danger"
    public bool IsRead { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private AdminNotification() { }

    public AdminNotification(string title, string message, string type = "info", bool isRead = false, Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        Title = title;
        Message = message;
        Type = type;
        IsRead = isRead;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsRead() => IsRead = true;
}
