using Shop.Domain.Common;
using Shop.Domain.Events;
using Shop.Domain.ValueObjects;

namespace Shop.Domain.Entities;

public class Product : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Sku Sku { get; private set; } = null!;
    public Money Price { get; private set; } = null!;
    public Money? DiscountPrice { get; private set; }
    public int StockQuantity { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Guid CategoryId { get; private set; }
    public Category Category { get; private set; } = null!;

    public Guid? BrandId { get; private set; }
    public Brand? Brand { get; private set; }

    public List<ProductImage> Images { get; private set; } = [];

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; private set; }

    private Product() { }

    public Product(
        string name,
        string slug,
        string description,
        Sku sku,
        Money price,
        Money? discountPrice,
        int initialStock,
        Guid categoryId,
        string categoryName,
        Guid? brandId = null,
        string? brandName = null,
        List<string>? imageUrls = null)
    {
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Description = description;
        Sku = sku;
        Price = price;
        DiscountPrice = discountPrice;
        StockQuantity = Math.Max(0, initialStock);
        CategoryId = categoryId;
        BrandId = brandId;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;

        if (imageUrls != null && imageUrls.Count > 0)
        {
            for (int i = 0; i < imageUrls.Count; i++)
            {
                Images.Add(new ProductImage(imageUrls[i], isPrimary: i == 0, displayOrder: i));
            }
        }

        AddDomainEvent(new ProductCreatedEvent(
            Id,
            Name,
            Slug,
            Sku.Value,
            Price.Amount,
            DiscountPrice?.Amount,
            StockQuantity,
            CategoryId,
            categoryName,
            BrandId,
            brandName,
            Images.Select(img => img.ImageUrl).ToList()
        ));
    }

    public void UpdateDetails(
        string name,
        string slug,
        string description,
        Money price,
        Money? discountPrice,
        Guid categoryId,
        string categoryName,
        Guid? brandId,
        string? brandName,
        bool isActive)
    {
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Description = description;
        Price = price;
        DiscountPrice = discountPrice;
        CategoryId = categoryId;
        BrandId = brandId;
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new ProductUpdatedEvent(
            Id,
            Name,
            Slug,
            Sku.Value,
            Price.Amount,
            DiscountPrice?.Amount,
            StockQuantity,
            CategoryId,
            categoryName,
            BrandId,
            brandName,
            IsActive,
            Images.Select(img => img.ImageUrl).ToList()
        ));
    }

    public bool DeductStock(int quantity)
    {
        if (quantity <= 0) return false;
        if (StockQuantity < quantity) return false;

        var oldStock = StockQuantity;
        StockQuantity -= quantity;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new ProductStockChangedEvent(Id, oldStock, StockQuantity));
        return true;
    }

    public void RestoreStock(int quantity)
    {
        if (quantity <= 0) return;
        var oldStock = StockQuantity;
        StockQuantity += quantity;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new ProductStockChangedEvent(Id, oldStock, StockQuantity));
    }

    public void SetStock(int newStock)
    {
        var oldStock = StockQuantity;
        StockQuantity = Math.Max(0, newStock);
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new ProductStockChangedEvent(Id, oldStock, StockQuantity));
    }

    public void SetImages(List<string> imageUrls)
    {
        Images.Clear();
        for (int i = 0; i < imageUrls.Count; i++)
        {
            Images.Add(new ProductImage(imageUrls[i], isPrimary: i == 0, displayOrder: i));
        }
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public decimal GetCurrentEffectivePrice() => DiscountPrice?.Amount ?? Price.Amount;
}

public class ProductImage : Entity<Guid>
{
    public Guid ProductId { get; private set; }
    public string ImageUrl { get; private set; } = string.Empty;
    public bool IsPrimary { get; private set; }
    public int DisplayOrder { get; private set; }

    private ProductImage() { }

    public ProductImage(string imageUrl, bool isPrimary = false, int displayOrder = 0)
    {
        ImageUrl = imageUrl;
        IsPrimary = isPrimary;
        DisplayOrder = displayOrder;
    }
}
