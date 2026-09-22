using Shop.Domain.Common;

namespace Shop.Domain.Entities;

public class OrderTimelineEvent : Entity<Guid>
{
    public Guid OrderId { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string PerformedBy { get; private set; } = string.Empty;
    public string Timestamp { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private OrderTimelineEvent() { }

    public OrderTimelineEvent(
        Guid orderId,
        string status,
        string title,
        string description,
        string performedBy,
        string? timestamp = null,
        Guid? id = null) : base(id ?? Guid.NewGuid())
    {
        OrderId = orderId;
        Status = status;
        Title = title;
        Description = description;
        PerformedBy = performedBy;
        Timestamp = timestamp ?? DateTime.Now.ToString("yyyy/MM/dd - HH:mm");
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
