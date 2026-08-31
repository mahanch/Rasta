namespace Shop.Application.DTOs;

public record AuthResponseDto(
    string Token,
    string RefreshToken,
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    DateTimeOffset ExpiresAt
);

public record UserProfileDto(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    string? PhoneNumber,
    DateTimeOffset CreatedAt
);

public record CategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? ImageUrl,
    bool IsActive,
    Guid? ParentCategoryId
);

public record ProductDto(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    string Sku,
    decimal Price,
    decimal? DiscountPrice,
    decimal EffectivePrice,
    int StockQuantity,
    bool InStock,
    bool IsActive,
    Guid CategoryId,
    string CategoryName,
    Guid? BrandId,
    string? BrandName,
    List<string> ImageUrls,
    DateTimeOffset CreatedAt
);

public record PaginatedResult<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

public record CartItemDto(
    Guid ProductId,
    string ProductName,
    string Sku,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice,
    string? ImageUrl
);

public record CartDto(
    Guid UserId,
    List<CartItemDto> Items,
    decimal TotalAmount,
    int TotalQuantity
);

public record AddressDto(
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country,
    string RecipientName,
    string PhoneNumber
);

public record OrderItemDetailDto(
    Guid ProductId,
    string ProductName,
    string Sku,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice,
    string? ImageUrl
);

public record OrderDto(
    Guid Id,
    string OrderNumber,
    Guid UserId,
    string Status,
    string PaymentStatus,
    decimal TotalAmount,
    decimal DiscountAmount,
    decimal FinalAmount,
    AddressDto ShippingAddress,
    List<OrderItemDetailDto> Items,
    string? PaymentTransactionId,
    string? CancellationReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt
);

public record ShopLicenseStatusDto(
    string LicenseKey,
    string Status,
    string Type,
    int? MaxOrders,
    int UsedOrders,
    int? RemainingOrders,
    DateTimeOffset? ExpirationDate,
    bool IsValid,
    DateTimeOffset LastCheckedAt,
    string Message
);
