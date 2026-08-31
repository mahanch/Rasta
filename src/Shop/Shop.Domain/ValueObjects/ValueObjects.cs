using Shop.Domain.Common;

namespace Shop.Domain.ValueObjects;

public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money() { Amount = 0; Currency = "USD"; }

    public Money(decimal amount, string currency = "USD")
    {
        if (amount < 0) throw new ArgumentException("Amount cannot be negative.", nameof(amount));
        Amount = Math.Round(amount, 2);
        Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.ToUpperInvariant();
    }

    public static Money Zero(string currency = "USD") => new(0, currency);

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency) throw new InvalidOperationException("Cannot add different currencies.");
        return new Money(a.Amount + b.Amount, a.Currency);
    }

    public static Money operator -(Money a, Money b)
    {
        if (a.Currency != b.Currency) throw new InvalidOperationException("Cannot subtract different currencies.");
        return new Money(a.Amount - b.Amount, a.Currency);
    }

    public static Money operator *(Money a, int quantity) => new(a.Amount * quantity, a.Currency);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}

public class Sku : ValueObject
{
    public string Value { get; }

    private Sku() { Value = string.Empty; }

    public Sku(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("SKU cannot be empty.", nameof(value));
        Value = value.Trim().ToUpperInvariant();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}

public class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string State { get; }
    public string PostalCode { get; }
    public string Country { get; }
    public string RecipientName { get; }
    public string PhoneNumber { get; }

    private Address()
    {
        Street = "";
        City = "";
        State = "";
        PostalCode = "";
        Country = "";
        RecipientName = "";
        PhoneNumber = "";
    }

    public Address(string street, string city, string state, string postalCode, string country, string recipientName, string phoneNumber)
    {
        Street = street;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
        RecipientName = recipientName;
        PhoneNumber = phoneNumber;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return PostalCode;
        yield return Country;
        yield return RecipientName;
        yield return PhoneNumber;
    }
}

public class LicenseState : ValueObject
{
    public string LicenseKey { get; }
    public string Status { get; }
    public string Type { get; }
    public int? MaxOrders { get; }
    public int UsedOrders { get; }
    public int? RemainingOrders { get; }
    public DateTimeOffset? ExpirationDate { get; }
    public bool IsValid { get; }
    public DateTimeOffset LastCheckedAt { get; }

    public LicenseState(
        string licenseKey,
        string status,
        string type,
        int? maxOrders,
        int usedOrders,
        int? remainingOrders,
        DateTimeOffset? expirationDate,
        bool isValid,
        DateTimeOffset lastCheckedAt)
    {
        LicenseKey = licenseKey;
        Status = status;
        Type = type;
        MaxOrders = maxOrders;
        UsedOrders = usedOrders;
        RemainingOrders = remainingOrders;
        ExpirationDate = expirationDate;
        IsValid = isValid;
        LastCheckedAt = lastCheckedAt;
    }

    public static LicenseState Default(string key = "DEFAULT-KEY") =>
        new(key, "PendingVerification", "Unknown", 100, 0, 100, null, true, DateTimeOffset.UtcNow);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return LicenseKey;
        yield return Status;
        yield return Type;
        yield return MaxOrders;
        yield return UsedOrders;
        yield return RemainingOrders;
        yield return ExpirationDate;
        yield return IsValid;
    }
}
