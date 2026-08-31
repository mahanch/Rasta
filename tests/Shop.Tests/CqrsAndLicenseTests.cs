using FluentAssertions;
using Moq;
using Shop.Application.Common.Interfaces;
using Shop.Application.Contracts;
using Shop.Application.DTOs;
using Shop.Application.Features.Orders.Commands;
using Shop.Application.Features.Payments.Commands;
using Shop.Domain.Common;
using Shop.Domain.Entities;
using Shop.Domain.Repositories;
using Shop.Domain.ValueObjects;
using Xunit;

namespace Shop.Tests.Application;

public class CqrsAndLicenseTests
{
    [Fact]
    public async Task Checkout_WhenLicenseLimitExceeded_ShouldReturnFailureResult()
    {
        // Arrange
        var mockCartRepo = new Mock<ICartRepository>();
        var mockProductRepo = new Mock<IProductRepository>();
        var mockOrderRepo = new Mock<IOrderRepository>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var mockLicenseService = new Mock<ILicenseClientService>();
        var mockEventPublisher = new Mock<IEventPublisher>();

        // License limit reached (e.g. 100/100 orders used)
        mockLicenseService.Setup(l => l.CanProcessOrderAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        mockLicenseService.Setup(l => l.GetCurrentLicenseState())
            .Returns(new LicenseState("SHOP-KEY", "QuotaExceeded", "OrderLimit", 100, 100, 0, null, false, DateTimeOffset.UtcNow));

        var handler = new CheckoutOrderCommandHandler(
            mockCartRepo.Object,
            mockProductRepo.Object,
            mockOrderRepo.Object,
            mockUnitOfWork.Object,
            mockLicenseService.Object,
            mockEventPublisher.Object
        );

        var address = new AddressDto("Street 1", "City", "State", "12345", "Country", "Recipient", "123456");
        var command = new CheckoutOrderCommand(Guid.NewGuid(), address);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("License.LimitExceeded");
        mockOrderRepo.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Checkout_WhenLicenseIsValid_ShouldCreateOrderAndPublishEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var mockCartRepo = new Mock<ICartRepository>();
        var mockProductRepo = new Mock<IProductRepository>();
        var mockOrderRepo = new Mock<IOrderRepository>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var mockLicenseService = new Mock<ILicenseClientService>();
        var mockEventPublisher = new Mock<IEventPublisher>();

        // License valid
        mockLicenseService.Setup(l => l.CanProcessOrderAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Cart with items
        var cart = new Cart(userId);
        cart.AddItem(productId, "MacBook Pro", "MBP-1", 2000m, 1, "mbp.jpg");
        mockCartRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        // Product with stock
        var product = new Product("MacBook Pro", "mbp", "Desc", new Sku("MBP-1"), new Money(2000m), null, 10, Guid.NewGuid(), "Laptops");
        mockProductRepo.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var handler = new CheckoutOrderCommandHandler(
            mockCartRepo.Object,
            mockProductRepo.Object,
            mockOrderRepo.Object,
            mockUnitOfWork.Object,
            mockLicenseService.Object,
            mockEventPublisher.Object
        );

        var address = new AddressDto("Street 1", "City", "State", "12345", "Country", "Recipient", "123456");
        var command = new CheckoutOrderCommand(userId, address);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.FinalAmount.Should().Be(2000m);
        result.Value.Status.Should().Be("Pending");

        // Verify stock deducted
        product.StockQuantity.Should().Be(9);

        // Verify order saved
        mockOrderRepo.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
        mockUnitOfWork.Verify(u => u.CommitChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Verify integration event published to RabbitMQ
        mockEventPublisher.Verify(p => p.PublishAsync(It.IsAny<OrderCreatedIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PaymentSuccess_ShouldMarkOrderPaidAndRecordLicenseUsage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order = Order.Create(
            userId,
            new Address("St", "City", "State", "123", "Country", "Name", "123"),
            [(Guid.NewGuid(), "Item", "SKU", 100m, 1, null)]
        );

        var mockOrderRepo = new Mock<IOrderRepository>();
        mockOrderRepo.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var mockPaymentRepo = new Mock<IPaymentRepository>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var mockLicenseService = new Mock<ILicenseClientService>();
        var mockEventPublisher = new Mock<IEventPublisher>();

        var handler = new SimulatePaymentSuccessCommandHandler(
            mockOrderRepo.Object,
            mockPaymentRepo.Object,
            mockUnitOfWork.Object,
            mockLicenseService.Object,
            mockEventPublisher.Object
        );

        var command = new SimulatePaymentSuccessCommand(order.Id, "TXN-TEST-123");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Paid);
        order.PaymentStatus.Should().Be(PaymentStatus.Success);

        // Verify License server called to record order usage
        mockLicenseService.Verify(l => l.RecordOrderUsageAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Verify OrderPaid event published
        mockEventPublisher.Verify(p => p.PublishAsync(It.IsAny<OrderPaidIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
