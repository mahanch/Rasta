using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Shop.Application.ReadModels;

public class ProductReadModel
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int StockQuantity { get; set; }
    public bool InStock => StockQuantity > 0;
    public bool IsActive { get; set; } = true;
    
    [BsonRepresentation(BsonType.String)]
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    
    [BsonRepresentation(BsonType.String)]
    public Guid? BrandId { get; set; }
    public string? BrandName { get; set; }

    public List<string> ImageUrls { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public class CategoryReadModel
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;

    [BsonRepresentation(BsonType.String)]
    public Guid? ParentCategoryId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class OrderReadModel
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public Guid UserId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;

    public string Status { get; set; } = "Pending";
    public string PaymentStatus { get; set; } = "Pending";

    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }

    public OrderAddressReadModel ShippingAddress { get; set; } = new();
    public List<OrderItemReadModel> Items { get; set; } = [];

    public string? PaymentTransactionId { get; set; }
    public string? CancellationReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
}

public class OrderAddressReadModel
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}

public class OrderItemReadModel
{
    [BsonRepresentation(BsonType.String)]
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public string? ImageUrl { get; set; }
}
