using Shop.Domain.Common;

namespace Shop.Domain.Entities;

public class Coupon : Entity<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public string Type { get; private set; } = "percent"; // "percent" | "fixed"
    public decimal Value { get; private set; }
    public decimal? MaxDiscount { get; private set; }
    public decimal MinCartSpend { get; private set; }
    public string StartDate { get; private set; } = string.Empty;
    public string EndDate { get; private set; } = string.Empty;
    public int TotalLimit { get; private set; } = 100;
    public int UsedCount { get; private set; }
    public int PerUserLimit { get; private set; } = 1;
    public List<string> TargetTiers { get; private set; } = []; // ["vip", "gold"]
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private Coupon() { }

    public Coupon(
        string code,
        string type,
        decimal value,
        decimal? maxDiscount,
        decimal minCartSpend,
        string startDate,
        string endDate,
        int totalLimit,
        int perUserLimit,
        List<string> targetTiers,
        bool isActive = true,
        Guid? id = null) : base(id ?? Guid.NewGuid())
    {
        Code = code.Trim().ToUpperInvariant();
        Type = type;
        Value = value;
        MaxDiscount = maxDiscount;
        MinCartSpend = minCartSpend;
        StartDate = startDate;
        EndDate = endDate;
        TotalLimit = totalLimit;
        PerUserLimit = perUserLimit;
        TargetTiers = targetTiers ?? [];
        IsActive = isActive;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Toggle() => IsActive = !IsActive;
    public void IncrementUsage() => UsedCount++;
}

public class Campaign : Entity<Guid>
{
    public string Title { get; private set; } = string.Empty;
    public string StartDate { get; private set; } = string.Empty;
    public string EndDate { get; private set; } = string.Empty;
    public int ClicksCount { get; private set; }
    public int OrdersCount { get; private set; }
    public decimal Revenue { get; private set; }
    public decimal SpendCost { get; private set; }
    public double Roi => SpendCost > 0 ? (double)((Revenue - SpendCost) / SpendCost * 100) : 0;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private Campaign() { }

    public Campaign(
        string title,
        string startDate,
        string endDate,
        int clicksCount,
        int ordersCount,
        decimal revenue,
        decimal spendCost,
        bool isActive = true,
        Guid? id = null) : base(id ?? Guid.NewGuid())
    {
        Title = title;
        StartDate = startDate;
        EndDate = endDate;
        ClicksCount = clicksCount;
        OrdersCount = ordersCount;
        Revenue = revenue;
        SpendCost = spendCost;
        IsActive = isActive;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}

public class AbandonedCartRecord : Entity<Guid>
{
    public Guid CartId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public string CustomerPhone { get; private set; } = string.Empty;
    public decimal TotalAmount { get; private set; }
    public int ItemCount { get; private set; }
    public string Status { get; private set; } = "pending"; // "pending", "reminder_sent", "recovered"
    public string? CouponSent { get; private set; }
    public DateTimeOffset LastActiveAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReminderSentAt { get; private set; }

    private AbandonedCartRecord() { }

    public AbandonedCartRecord(
        Guid cartId,
        Guid? customerId,
        string customerName,
        string customerPhone,
        decimal totalAmount,
        int itemCount,
        DateTimeOffset lastActiveAt,
        Guid? id = null) : base(id ?? Guid.NewGuid())
    {
        CartId = cartId;
        CustomerId = customerId;
        CustomerName = customerName;
        CustomerPhone = customerPhone;
        TotalAmount = totalAmount;
        ItemCount = itemCount;
        LastActiveAt = lastActiveAt;
        Status = "pending";
    }

    public void MarkReminderSent(string couponCode)
    {
        Status = "reminder_sent";
        CouponSent = couponCode;
        ReminderSentAt = DateTimeOffset.UtcNow;
    }
}
