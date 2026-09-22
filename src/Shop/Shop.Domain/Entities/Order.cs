using Shop.Domain.Common;
using Shop.Domain.Events;
using Shop.Domain.ValueObjects;

namespace Shop.Domain.Entities;

public enum OrderStatus
{
    Pending = 1,
    Paid = 2,
    Processing = 3,
    Shipped = 4,
    Delivered = 5,
    Cancelled = 6
}

public enum PaymentStatus
{
    Pending = 1,
    Success = 2,
    Failed = 3,
    Refunded = 4
}

public class Order : AggregateRoot<Guid>
{
    public string OrderNumber { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    
    public OrderStatus Status { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }

    public Address ShippingAddress { get; private set; } = null!;

    public decimal TotalAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal FinalAmount { get; private set; }

    public string? PaymentTransactionId { get; private set; }
    public string? CancellationReason { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PaidAt { get; private set; }
    public DateTimeOffset? ShippedAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }

    public List<OrderItem> Items { get; private set; } = [];

    private Order() { }

    public static Order Create(
        Guid userId,
        Address shippingAddress,
        List<(Guid productId, string productName, string sku, decimal unitPrice, int quantity, string? imageUrl)> items,
        decimal discountAmount = 0)
    {
        if (items == null || items.Count == 0)
            throw new ArgumentException("Order must contain at least one item.", nameof(items));

        var order = new Order
        {
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..22].ToUpperInvariant(),
            UserId = userId,
            ShippingAddress = shippingAddress,
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
            DiscountAmount = Math.Max(0, discountAmount),
            CreatedAt = DateTimeOffset.UtcNow
        };

        foreach (var item in items)
        {
            order.Items.Add(new OrderItem(order.Id, item.productId, item.productName, item.sku, item.unitPrice, item.quantity, item.imageUrl));
        }

        order.TotalAmount = order.Items.Sum(i => i.TotalPrice);
        order.FinalAmount = Math.Max(0, order.TotalAmount - order.DiscountAmount);

        order.AddDomainEvent(new OrderCreatedEvent(
            order.Id,
            order.OrderNumber,
            order.UserId,
            order.TotalAmount,
            order.DiscountAmount,
            order.FinalAmount,
            order.Status.ToString(),
            order.PaymentStatus.ToString(),
            order.ShippingAddress,
            order.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.Sku, i.UnitPrice, i.Quantity, i.TotalPrice, i.ImageUrl)).ToList(),
            order.CreatedAt
        ));

        return order;
    }

    public void MarkAsPaid(string transactionReference)
    {
        if (Status == OrderStatus.Cancelled)
            throw new InvalidOperationException("Cannot pay for a cancelled order.");

        var prevStatus = Status;
        Status = OrderStatus.Paid;
        PaymentStatus = PaymentStatus.Success;
        PaymentTransactionId = transactionReference;
        PaidAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new OrderPaidEvent(Id, OrderNumber, UserId, FinalAmount, transactionReference, PaidAt.Value));
        AddDomainEvent(new OrderStatusChangedEvent(Id, OrderNumber, prevStatus.ToString(), Status.ToString(), PaidAt.Value));
    }

    public void UpdateStatus(OrderStatus newStatus)
    {
        if (Status == OrderStatus.Cancelled && newStatus != OrderStatus.Cancelled)
            throw new InvalidOperationException("Cannot change status of a cancelled order.");

        var prevStatus = Status;
        Status = newStatus;

        if (newStatus == OrderStatus.Shipped) ShippedAt = DateTimeOffset.UtcNow;
        if (newStatus == OrderStatus.Delivered) DeliveredAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new OrderStatusChangedEvent(Id, OrderNumber, prevStatus.ToString(), Status.ToString(), DateTimeOffset.UtcNow));
    }

    public void Cancel(string reason)
    {
        if (Status == OrderStatus.Shipped || Status == OrderStatus.Delivered)
            throw new InvalidOperationException("Cannot cancel an already shipped or delivered order.");

        var prevStatus = Status;
        Status = OrderStatus.Cancelled;
        CancellationReason = reason;
        CancelledAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new OrderCancelledEvent(Id, OrderNumber, reason, CancelledAt.Value));
        AddDomainEvent(new OrderStatusChangedEvent(Id, OrderNumber, prevStatus.ToString(), Status.ToString(), CancelledAt.Value));
    }
}

public class OrderItem : Entity<Guid>
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal TotalPrice { get; private set; }
    public string? ImageUrl { get; private set; }

    private OrderItem() { }

    public OrderItem(Guid orderId, Guid productId, string productName, string sku, decimal unitPrice, int quantity, string? imageUrl)
    {
        OrderId = orderId;
        ProductId = productId;
        ProductName = productName;
        Sku = sku;
        UnitPrice = unitPrice;
        Quantity = quantity;
        TotalPrice = unitPrice * quantity;
        ImageUrl = imageUrl;
    }
}
