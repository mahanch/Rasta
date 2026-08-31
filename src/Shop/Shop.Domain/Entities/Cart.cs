using Shop.Domain.Common;

namespace Shop.Domain.Entities;

public class Cart : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public List<CartItem> Items { get; private set; } = [];
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; private set; }

    private Cart() { }

    public Cart(Guid userId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void AddItem(Guid productId, string productName, string sku, decimal unitPrice, int quantity, string? imageUrl)
    {
        var existing = Items.FirstOrDefault(i => i.ProductId == productId);
        if (existing != null)
        {
            existing.AddQuantity(quantity);
        }
        else
        {
            Items.Add(new CartItem(Id, productId, productName, sku, unitPrice, quantity, imageUrl));
        }

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateItemQuantity(Guid productId, int quantity)
    {
        var item = Items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null) return;

        if (quantity <= 0)
        {
            Items.Remove(item);
        }
        else
        {
            item.SetQuantity(quantity);
        }

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RemoveItem(Guid productId)
    {
        var item = Items.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
        {
            Items.Remove(item);
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public void Clear()
    {
        Items.Clear();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public decimal TotalAmount => Items.Sum(i => i.TotalPrice);
    public int TotalQuantity => Items.Sum(i => i.Quantity);
}

public class CartItem : Entity<Guid>
{
    public Guid CartId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public string? ImageUrl { get; private set; }

    public decimal TotalPrice => UnitPrice * Quantity;

    private CartItem() { }

    public CartItem(Guid cartId, Guid productId, string productName, string sku, decimal unitPrice, int quantity, string? imageUrl)
    {
        Id = Guid.NewGuid();
        CartId = cartId;
        ProductId = productId;
        ProductName = productName;
        Sku = sku;
        UnitPrice = unitPrice;
        Quantity = Math.Max(1, quantity);
        ImageUrl = imageUrl;
    }

    public void AddQuantity(int qty)
    {
        Quantity += qty;
    }

    public void SetQuantity(int qty)
    {
        Quantity = Math.Max(1, qty);
    }
}
