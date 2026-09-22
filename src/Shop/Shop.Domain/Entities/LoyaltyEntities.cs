using Shop.Domain.Common;

namespace Shop.Domain.Entities;

public class LoyaltyRule : Entity<Guid>
{
    public int PurchaseRatio { get; set; } = 10; // به ازای هر ۱۰۰ هزار تومان ۱۰ امتیاز
    public int RegisterPoints { get; set; } = 100; // هدیه عضویت
    public int ReviewPoints { get; set; } = 50; // پاداش ثبت دیدگاه
    public int ReferralPoints { get; set; } = 200; // پاداش معرفی خریدار جدید
    public int PointExpiryDays { get; set; } = 365; // انقضای یکساله

    public LoyaltyRule() : base(Guid.NewGuid()) { }
}

public class LoyaltyTier : Entity<Guid>
{
    public string TierKey { get; private set; } = "bronze"; // bronze, silver, gold, vip
    public string NameFa { get; private set; } = "برنزی";
    public decimal MinSpend { get; private set; }
    public int MinPoints { get; private set; }
    public int PermanentDiscount { get; private set; }
    public List<string> Perks { get; private set; } = [];

    private LoyaltyTier() { }

    public LoyaltyTier(
        string tierKey,
        string nameFa,
        decimal minSpend,
        int minPoints,
        int permanentDiscount,
        List<string> perks,
        Guid? id = null) : base(id ?? Guid.NewGuid())
    {
        TierKey = tierKey.ToLowerInvariant();
        NameFa = nameFa;
        MinSpend = minSpend;
        MinPoints = minPoints;
        PermanentDiscount = permanentDiscount;
        Perks = perks ?? [];
    }

    public void Update(decimal minSpend, int minPoints, int permanentDiscount, List<string> perks)
    {
        MinSpend = minSpend;
        MinPoints = minPoints;
        PermanentDiscount = permanentDiscount;
        Perks = perks ?? [];
    }
}

public class LoyaltyReward : Entity<Guid>
{
    public string Title { get; private set; } = string.Empty;
    public int PointCost { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string DiscountType { get; private set; } = "percent"; // percent, fixed
    public decimal DiscountValue { get; private set; }
    public int ExpirationDays { get; private set; } = 30;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private LoyaltyReward() { }

    public LoyaltyReward(
        string title,
        int pointCost,
        string description,
        string discountType,
        decimal discountValue,
        int expirationDays,
        bool isActive = true,
        Guid? id = null) : base(id ?? Guid.NewGuid())
    {
        Title = title.Trim();
        PointCost = pointCost;
        Description = description;
        DiscountType = discountType;
        DiscountValue = discountValue;
        ExpirationDays = expirationDays;
        IsActive = isActive;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Toggle() => IsActive = !IsActive;
}

public class LoyaltyTransaction : Entity<Guid>
{
    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public int Points { get; private set; }
    public string Type { get; private set; } = "earned"; // "earned" | "spent"
    public string Description { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private LoyaltyTransaction() { }

    public LoyaltyTransaction(
        Guid customerId,
        string customerName,
        int points,
        string type,
        string description,
        Guid? id = null) : base(id ?? Guid.NewGuid())
    {
        CustomerId = customerId;
        CustomerName = customerName;
        Points = points;
        Type = type;
        Description = description;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
