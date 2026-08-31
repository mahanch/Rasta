using FluentAssertions;
using LicenseServer.Domain.Entities;
using LicenseServer.Domain.Enums;
using LicenseServer.Infrastructure.Services;
using Shop.Domain.Entities;
using Shop.Domain.ValueObjects;
using Xunit;
using LicenseEntity = LicenseServer.Domain.Entities.License;

namespace Shop.Tests.Domain;

public class DomainTests
{
    [Fact]
    public void Money_ShouldPerformCorrectArithmetic_AndThrowOnNegative()
    {
        var m1 = new Money(100.50m);
        var m2 = new Money(50.25m);

        var sum = m1 + m2;
        sum.Amount.Should().Be(150.75m);

        var diff = m1 - m2;
        diff.Amount.Should().Be(50.25m);

        var multiplied = m2 * 3;
        multiplied.Amount.Should().Be(150.75m);

        var act = () => new Money(-10);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Product_StockDeduction_ShouldUpdateStockAndInvariants()
    {
        var product = new Product(
            "Test Laptop",
            "test-laptop",
            "Description",
            new Sku("TEST-SKU-1"),
            new Money(1000m),
            new Money(900m),
            10,
            Guid.NewGuid(),
            "Laptops"
        );

        product.StockQuantity.Should().Be(10);
        product.GetCurrentEffectivePrice().Should().Be(900m);

        var success = product.DeductStock(4);
        success.Should().BeTrue();
        product.StockQuantity.Should().Be(6);

        var failed = product.DeductStock(10);
        failed.Should().BeFalse();
        product.StockQuantity.Should().Be(6);

        product.RestoreStock(2);
        product.StockQuantity.Should().Be(8);
    }

    [Fact]
    public void Cart_ShouldAddAndUpdateItemsCorrectly()
    {
        var userId = Guid.NewGuid();
        var cart = new Cart(userId);
        var prodId = Guid.NewGuid();

        cart.AddItem(prodId, "Phone", "PH-1", 500m, 2, "img.jpg");
        cart.TotalQuantity.Should().Be(2);
        cart.TotalAmount.Should().Be(1000m);

        cart.AddItem(prodId, "Phone", "PH-1", 500m, 1, "img.jpg");
        cart.TotalQuantity.Should().Be(3);
        cart.TotalAmount.Should().Be(1500m);

        cart.UpdateItemQuantity(prodId, 1);
        cart.TotalQuantity.Should().Be(1);
        cart.TotalAmount.Should().Be(500m);

        cart.Clear();
        cart.Items.Should().BeEmpty();
        cart.TotalAmount.Should().Be(0);
    }

    [Fact]
    public void Order_CreateAndStateTransitions_ShouldEnforceInvariants()
    {
        var userId = Guid.NewGuid();
        var address = new Address("Main St", "Tehran", "Tehran", "12345", "Iran", "Reza", "+989120000000");
        var items = new List<(Guid productId, string productName, string sku, decimal unitPrice, int quantity, string? imageUrl)>
        {
            (Guid.NewGuid(), "Headphones", "HP-1", 100m, 2, "hp.jpg")
        };

        var order = Order.Create(userId, address, items, discountAmount: 10m);

        order.Status.Should().Be(OrderStatus.Pending);
        order.TotalAmount.Should().Be(200m);
        order.DiscountAmount.Should().Be(10m);
        order.FinalAmount.Should().Be(190m);

        order.MarkAsPaid("TXN-12345");
        order.Status.Should().Be(OrderStatus.Paid);
        order.PaymentStatus.Should().Be(PaymentStatus.Success);
        order.PaidAt.Should().NotBeNull();

        order.UpdateStatus(OrderStatus.Shipped);
        order.Status.Should().Be(OrderStatus.Shipped);

        // Cannot cancel shipped order
        var act = () => order.Cancel("Changed mind");
        act.Should().Throw<InvalidOperationException>();
    }
}

public class LicenseTests
{
    [Fact]
    public void OrderLimitLicense_ShouldBeValidUntilQuotaReached()
    {
        var tenantId = Guid.NewGuid();
        var license = LicenseEntity.CreateOrderLimitLicense(tenantId, maxOrders: 2);

        license.Type.Should().Be(LicenseType.OrderLimit);
        license.IsValid().Should().BeTrue();
        license.CanProcessOrder().Should().BeTrue();

        // Order 1
        var rec1 = license.RecordOrder();
        rec1.Should().BeTrue();
        license.UsedOrders.Should().Be(1);
        license.IsValid().Should().BeTrue();

        // Order 2
        var rec2 = license.RecordOrder();
        rec2.Should().BeTrue();
        license.UsedOrders.Should().Be(2);

        // Quota reached (2/2) -> CanProcessOrder should be false
        license.CanProcessOrder().Should().BeFalse();
        var rec3 = license.RecordOrder();
        rec3.Should().BeFalse();
        license.UsedOrders.Should().Be(2);

        // Upgrade to Lifetime
        license.UpgradeToLifetime();
        license.Type.Should().Be(LicenseType.Lifetime);
        license.CanProcessOrder().Should().BeTrue();

        var rec4 = license.RecordOrder();
        rec4.Should().BeTrue();
        license.UsedOrders.Should().Be(3);
    }

    [Fact]
    public void TimeLimitLicense_ShouldExpireAfterDate()
    {
        var tenantId = Guid.NewGuid();
        var license = LicenseEntity.CreateTimeLimitLicense(tenantId, DateTimeOffset.UtcNow.AddSeconds(-10));

        license.IsValid().Should().BeFalse();
        license.CanProcessOrder().Should().BeFalse();

        // Extend expiration
        license.ExtendExpiration(DateTimeOffset.UtcNow.AddDays(30));
        license.IsValid().Should().BeTrue();
        license.CanProcessOrder().Should().BeTrue();
    }

    [Fact]
    public void LicenseCryptoService_ShouldSignAndVerifyValidPayloads()
    {
        var crypto = new LicenseCryptoService("TEST_SECRET_KEY_FOR_UNIT_TESTS!");
        var signed = crypto.SignLicense(
            "TEST-KEY-123",
            "Active",
            "OrderLimit",
            100,
            25,
            null,
            true
        );

        signed.Signature.Should().NotBeNullOrWhiteSpace();

        var isValid = crypto.VerifySignature(signed);
        isValid.Should().BeTrue();

        // Tamper test
        var tampered = signed with { UsedOrders = 999 };
        var isTamperedValid = crypto.VerifySignature(tampered);
        isTamperedValid.Should().BeFalse();
    }
}
