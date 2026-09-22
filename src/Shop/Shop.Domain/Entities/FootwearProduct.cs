using Shop.Domain.Common;

namespace Shop.Domain.Entities;

public class FootwearSpecs
{
    public string Material { get; set; } = string.Empty;
    public string LeatherType { get; set; } = string.Empty;
    public string Tannery { get; set; } = string.Empty;
    public string SoleMaterial { get; set; } = string.Empty;
    public string Construction { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
}

public class FootwearSeo
{
    public string Title { get; set; } = string.Empty;
    public string MetaDescription { get; set; } = string.Empty;
    public string CanonicalUrl { get; set; } = string.Empty;
    public string FocusKeyword { get; set; } = string.Empty;
}

public class FootwearVariant : Entity<Guid>
{
    public Guid FootwearProductId { get; private set; }
    public int Size { get; private set; } // 39, 40, 41, 42, 43, 44, 45
    public string ColorName { get; private set; } = string.Empty;
    public string ColorHex { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public int Stock { get; private set; }
    public int LowStockThreshold { get; private set; } = 3;

    private FootwearVariant() { }

    public FootwearVariant(
        int size,
        string colorName,
        string colorHex,
        string sku,
        int stock,
        int lowStockThreshold = 3,
        Guid? id = null) : base(id ?? Guid.NewGuid())
    {
        Size = size;
        ColorName = colorName;
        ColorHex = colorHex;
        Sku = sku;
        Stock = Math.Max(0, stock);
        LowStockThreshold = lowStockThreshold;
    }

    public void UpdateStock(int newStock)
    {
        Stock = Math.Max(0, newStock);
    }

    public void UpdateDetails(string colorName, string colorHex, string sku, int lowStockThreshold)
    {
        ColorName = colorName;
        ColorHex = colorHex;
        Sku = sku;
        LowStockThreshold = lowStockThreshold;
    }

    public string StockStatus => Stock <= 0 ? "out_of_stock" : (Stock <= LowStockThreshold ? "low_stock" : "in_stock");
}

public class FootwearProduct : AggregateRoot<Guid>
{
    public string PersianName { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public string Category { get; private set; } = "formal";
    public string CategoryName { get; private set; } = "کفش رسمی";
    public string Collection { get; private set; } = "کفش‌های دست‌دوز شاهکار";
    public string Gender { get; private set; } = "men";
    public decimal BasePrice { get; private set; }
    public decimal? DiscountPrice { get; private set; }
    public decimal CostPrice { get; private set; }

    public FootwearSpecs Specs { get; private set; } = new();
    public FootwearSeo Seo { get; private set; } = new();

    public List<FootwearVariant> Variants { get; private set; } = [];
    public List<string> Images { get; private set; } = [];
    public string ShortDescription { get; private set; } = string.Empty;
    public string FullDescription { get; private set; } = string.Empty;
    public List<string> CareInstructions { get; private set; } = [];
    public string Status { get; private set; } = "published"; // "published", "draft", "archived"

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; private set; }

    private FootwearProduct() { }

    public FootwearProduct(
        string persianName,
        string name,
        string slug,
        string sku,
        string category,
        string categoryName,
        string collection,
        string gender,
        decimal basePrice,
        decimal? discountPrice,
        decimal costPrice,
        FootwearSpecs specs,
        FootwearSeo seo,
        List<FootwearVariant> variants,
        List<string> images,
        string shortDescription,
        string fullDescription,
        List<string> careInstructions,
        string status = "published",
        Guid? id = null) : base(id ?? Guid.NewGuid())
    {
        PersianName = persianName.Trim();
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Sku = sku.Trim();
        Category = category;
        CategoryName = categoryName;
        Collection = collection;
        Gender = gender;
        BasePrice = basePrice;
        DiscountPrice = discountPrice;
        CostPrice = costPrice;
        Specs = specs ?? new FootwearSpecs();
        Seo = seo ?? new FootwearSeo();
        Variants = variants ?? [];
        Images = images ?? [];
        ShortDescription = shortDescription;
        FullDescription = fullDescription;
        CareInstructions = careInstructions ?? [];
        Status = status;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public int TotalStock => Variants.Sum(v => v.Stock);

    public string StockStatus
    {
        get
        {
            if (Variants.Count == 0 || TotalStock == 0) return "out_of_stock";
            if (Variants.Any(v => v.Stock <= v.LowStockThreshold)) return "low_stock";
            return "in_stock";
        }
    }

    public void Update(
        string persianName,
        string name,
        string slug,
        string sku,
        string category,
        string categoryName,
        string collection,
        string gender,
        decimal basePrice,
        decimal? discountPrice,
        decimal costPrice,
        FootwearSpecs specs,
        FootwearSeo seo,
        List<string> images,
        string shortDescription,
        string fullDescription,
        List<string> careInstructions,
        string status)
    {
        PersianName = persianName.Trim();
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Sku = sku.Trim();
        Category = category;
        CategoryName = categoryName;
        Collection = collection;
        Gender = gender;
        BasePrice = basePrice;
        DiscountPrice = discountPrice;
        CostPrice = costPrice;
        Specs = specs ?? Specs;
        Seo = seo ?? Seo;
        Images = images ?? Images;
        ShortDescription = shortDescription;
        FullDescription = fullDescription;
        CareInstructions = careInstructions ?? CareInstructions;
        Status = status;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetStatus(string newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool UpdateVariantStock(Guid variantId, int newStock)
    {
        var variant = Variants.FirstOrDefault(v => v.Id == variantId);
        if (variant == null) return false;
        variant.UpdateStock(newStock);
        UpdatedAt = DateTimeOffset.UtcNow;
        return true;
    }

    public void ReplaceVariants(List<FootwearVariant> newVariants)
    {
        Variants.Clear();
        Variants.AddRange(newVariants);
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
