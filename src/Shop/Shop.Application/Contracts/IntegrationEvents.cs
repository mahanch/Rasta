namespace Shop.Application.Contracts;

public record ProductCreatedIntegrationEvent(
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
    List<string> ImageUrls,
    DateTimeOffset CreatedAt
);

public record ProductUpdatedIntegrationEvent(
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
    List<string> ImageUrls,
    DateTimeOffset UpdatedAt
);

public record ProductStockChangedIntegrationEvent(
    Guid ProductId,
    int OldStock,
    int NewStock,
    DateTimeOffset Timestamp
);

public record CategoryCreatedOrUpdatedIntegrationEvent(
    Guid CategoryId,
    string Name,
    string Slug,
    string? Description,
    string? ImageUrl,
    bool IsActive,
    Guid? ParentCategoryId,
    DateTimeOffset Timestamp
);

public record OrderCreatedIntegrationEvent(
    Guid OrderId,
    string OrderNumber,
    Guid UserId,
    decimal TotalAmount,
    decimal DiscountAmount,
    decimal FinalAmount,
    string Status,
    string PaymentStatus,
    string RecipientName,
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country,
    string PhoneNumber,
    List<OrderItemIntegrationDto> Items,
    DateTimeOffset CreatedAt
);

public record OrderItemIntegrationDto(
    Guid ProductId,
    string ProductName,
    string Sku,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice,
    string? ImageUrl
);

public record OrderPaidIntegrationEvent(
    Guid OrderId,
    string OrderNumber,
    Guid UserId,
    decimal FinalAmount,
    string TransactionReference,
    DateTimeOffset PaidAt
);

public record OrderStatusChangedIntegrationEvent(
    Guid OrderId,
    string OrderNumber,
    string PreviousStatus,
    string NewStatus,
    DateTimeOffset ChangedAt
);

public record OrderCancelledIntegrationEvent(
    Guid OrderId,
    string OrderNumber,
    string Reason,
    DateTimeOffset CancelledAt
);
