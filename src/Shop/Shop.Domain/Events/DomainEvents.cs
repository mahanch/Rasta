using Shop.Domain.Common;
using Shop.Domain.ValueObjects;

namespace Shop.Domain.Events;

// Catalog Events
public record ProductCreatedEvent(
    Guid ProductId,
    string Name,
    string Slug,
    string Sku,
    decimal Price,
    decimal? DiscountPrice,
    int StockQuantity,
    Guid CategoryId,
    string CategoryName,
    Guid? BrandId,
    string? BrandName,
    List<string> ImageUrls
) : IDomainEvent;

public record ProductUpdatedEvent(
    Guid ProductId,
    string Name,
    string Slug,
    string Sku,
    decimal Price,
    decimal? DiscountPrice,
    int StockQuantity,
    Guid CategoryId,
    string CategoryName,
    Guid? BrandId,
    string? BrandName,
    bool IsActive,
    List<string> ImageUrls
) : IDomainEvent;

public record ProductStockChangedEvent(
    Guid ProductId,
    int OldStock,
    int NewStock
) : IDomainEvent;

public record CategoryCreatedOrUpdatedEvent(
    Guid CategoryId,
    string Name,
    string Slug,
    string? Description,
    string? ImageUrl,
    bool IsActive,
    Guid? ParentCategoryId
) : IDomainEvent;

// Order Events
public record OrderCreatedEvent(
    Guid OrderId,
    string OrderNumber,
    Guid UserId,
    decimal TotalAmount,
    decimal DiscountAmount,
    decimal FinalAmount,
    string Status,
    string PaymentStatus,
    Address ShippingAddress,
    List<OrderItemDto> Items,
    DateTimeOffset CreatedAt
) : IDomainEvent;

public record OrderPaidEvent(
    Guid OrderId,
    string OrderNumber,
    Guid UserId,
    decimal FinalAmount,
    string TransactionReference,
    DateTimeOffset PaidAt
) : IDomainEvent;

public record OrderStatusChangedEvent(
    Guid OrderId,
    string OrderNumber,
    string PreviousStatus,
    string NewStatus,
    DateTimeOffset ChangedAt
) : IDomainEvent;

public record OrderCancelledEvent(
    Guid OrderId,
    string OrderNumber,
    string Reason,
    DateTimeOffset CancelledAt
) : IDomainEvent;

public record OrderItemDto(
    Guid ProductId,
    string ProductName,
    string Sku,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice,
    string? ImageUrl
);
