using Shop.Domain.Common;

namespace Shop.Domain.Entities;

public class CustomerNote : Entity<Guid>
{
    public Guid CustomerId { get; private set; }
    public string AdminName { get; private set; } = string.Empty;
    public string Note { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private CustomerNote() { }

    public CustomerNote(Guid customerId, string adminName, string note, Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        CustomerId = customerId;
        AdminName = adminName;
        Note = note.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
    }
}

public class CustomerSegment : Entity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Criteria { get; private set; } = string.Empty;
    public int CustomerCount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private CustomerSegment() { }

    public CustomerSegment(string name, string description, string criteria, int customerCount = 0, Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        Name = name.Trim();
        Description = description;
        Criteria = criteria;
        CustomerCount = customerCount;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateCount(int count) => CustomerCount = count;
}

public class CustomerProfile : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public bool IsVip { get; private set; }
    public string Tier { get; private set; } = "bronze"; // bronze, silver, gold, vip
    public decimal TotalSpent { get; private set; }
    public int OrdersCount { get; private set; }
    public decimal WalletBalance { get; private set; }
    public int LoyaltyPoints { get; private set; }
    public int? PreferredSize { get; private set; }
    public string PreferredColors { get; private set; } = string.Empty; // e.g. "قهوه‌ای تیره, عسلی"
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastOrderDate { get; private set; }

    private CustomerProfile() { }

    public CustomerProfile(
        Guid userId,
        string fullName,
        string phone,
        string email,
        bool isVip = false,
        string tier = "bronze",
        decimal totalSpent = 0,
        int ordersCount = 0,
        decimal walletBalance = 0,
        int loyaltyPoints = 0,
        int? preferredSize = 42,
        string preferredColors = "قهوه‌ای تیره",
        Guid? id = null) : base(id ?? Guid.NewGuid())
    {
        UserId = userId;
        FullName = fullName;
        Phone = phone;
        Email = email;
        IsVip = isVip;
        Tier = tier;
        TotalSpent = totalSpent;
        OrdersCount = ordersCount;
        WalletBalance = walletBalance;
        LoyaltyPoints = loyaltyPoints;
        PreferredSize = preferredSize;
        PreferredColors = preferredColors;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void RecordOrder(decimal amount)
    {
        TotalSpent += amount;
        OrdersCount += 1;
        LastOrderDate = DateTimeOffset.UtcNow;
        RecalculateTier();
    }

    public void UpdateWallet(decimal delta)
    {
        WalletBalance += delta;
    }

    public void UpdateLoyaltyPoints(int delta)
    {
        LoyaltyPoints = Math.Max(0, LoyaltyPoints + delta);
        RecalculateTier();
    }

    public void SetPreferences(int? size, string colors)
    {
        PreferredSize = size;
        PreferredColors = colors;
    }

    private void RecalculateTier()
    {
        if (TotalSpent >= 45000000 || LoyaltyPoints >= 2500)
        {
            Tier = "vip";
            IsVip = true;
        }
        else if (TotalSpent >= 25000000 || LoyaltyPoints >= 1200)
        {
            Tier = "gold";
            IsVip = false;
        }
        else if (TotalSpent >= 10000000 || LoyaltyPoints >= 500)
        {
            Tier = "silver";
            IsVip = false;
        }
        else
        {
            Tier = "bronze";
            IsVip = false;
        }
    }
}
