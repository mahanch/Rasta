using MediatR;
using Shop.Application.Common.Interfaces;
using Shop.Application.Contracts;
using Shop.Application.DTOs;
using Shop.Domain.Common;
using Shop.Domain.Entities;
using Shop.Domain.Repositories;
using Shop.Domain.ValueObjects;

namespace Shop.Application.Features.Orders.Commands;

public record CheckoutOrderCommand(
    Guid UserId,
    AddressDto ShippingAddress,
    decimal DiscountAmount = 0
) : IRequest<Result<OrderDto>>;

public class CheckoutOrderCommandHandler : IRequestHandler<CheckoutOrderCommand, Result<OrderDto>>
{
    private readonly ICartRepository _cartRepo;
    private readonly IProductRepository _productRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILicenseClientService _licenseService;
    private readonly IEventPublisher _eventPublisher;

    public CheckoutOrderCommandHandler(
        ICartRepository cartRepo,
        IProductRepository productRepo,
        IOrderRepository orderRepo,
        IUnitOfWork unitOfWork,
        ILicenseClientService licenseService,
        IEventPublisher eventPublisher)
    {
        _cartRepo = cartRepo;
        _productRepo = productRepo;
        _orderRepo = orderRepo;
        _unitOfWork = unitOfWork;
        _licenseService = licenseService;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result<OrderDto>> Handle(CheckoutOrderCommand request, CancellationToken cancellationToken)
    {
        // 1. License Quota & Validity Check (Enterprise Protection)
        var canProcessOrder = await _licenseService.CanProcessOrderAsync(cancellationToken);
        if (!canProcessOrder)
        {
            var licenseState = _licenseService.GetCurrentLicenseState();
            return Result<OrderDto>.Failure(new Error(
                "License.LimitExceeded",
                $"Store operations restricted. License status: '{licenseState.Status}', Used: {licenseState.UsedOrders}/{licenseState.MaxOrders}. Please contact support or upgrade your store license."
            ));
        }

        // 2. Load Cart
        var cart = await _cartRepo.GetByUserIdAsync(request.UserId, cancellationToken);
        if (cart == null || cart.Items.Count == 0)
        {
            return Result<OrderDto>.Failure(new Error("Order.EmptyCart", "Cannot checkout an empty cart."));
        }

        // 3. Validate Stock for all items
        var orderItemsData = new List<(Guid productId, string productName, string sku, decimal unitPrice, int quantity, string? imageUrl)>();
        foreach (var item in cart.Items)
        {
            var product = await _productRepo.GetByIdAsync(item.ProductId, cancellationToken);
            if (product == null || !product.IsActive)
            {
                return Result<OrderDto>.Failure(new Error("Order.ProductUnavailable", $"Product '{item.ProductName}' is no longer available."));
            }

            if (product.StockQuantity < item.Quantity)
            {
                return Result<OrderDto>.Failure(new Error("Order.InsufficientStock", $"Product '{product.Name}' only has {product.StockQuantity} in stock."));
            }

            // Deduct stock in transactional write model
            product.DeductStock(item.Quantity);
            orderItemsData.Add((product.Id, product.Name, product.Sku.Value, item.UnitPrice, item.Quantity, item.ImageUrl));
        }

        // 4. Create Order Aggregate
        var shippingAddressVo = new Address(
            request.ShippingAddress.Street,
            request.ShippingAddress.City,
            request.ShippingAddress.State,
            request.ShippingAddress.PostalCode,
            request.ShippingAddress.Country,
            request.ShippingAddress.RecipientName,
            request.ShippingAddress.PhoneNumber
        );

        var order = Order.Create(request.UserId, shippingAddressVo, orderItemsData, request.DiscountAmount);

        await _orderRepo.AddAsync(order, cancellationToken);

        // 5. Clear user cart
        cart.Clear();

        await _unitOfWork.CommitChangesAsync(cancellationToken);

        // 6. Publish Integration Event to RabbitMQ (for Mongo read projection)
        await _eventPublisher.PublishAsync(new OrderCreatedIntegrationEvent(
            order.Id,
            order.OrderNumber,
            order.UserId,
            order.TotalAmount,
            order.DiscountAmount,
            order.FinalAmount,
            order.Status.ToString(),
            order.PaymentStatus.ToString(),
            shippingAddressVo.RecipientName,
            shippingAddressVo.Street,
            shippingAddressVo.City,
            shippingAddressVo.State,
            shippingAddressVo.PostalCode,
            shippingAddressVo.Country,
            shippingAddressVo.PhoneNumber,
            order.Items.Select(i => new OrderItemIntegrationDto(i.ProductId, i.ProductName, i.Sku, i.UnitPrice, i.Quantity, i.TotalPrice, i.ImageUrl)).ToList(),
            order.CreatedAt
        ), cancellationToken);

        return Result<OrderDto>.Success(MapToDto(order));
    }

    private static OrderDto MapToDto(Order o)
    {
        return new OrderDto(
            o.Id,
            o.OrderNumber,
            o.UserId,
            o.Status.ToString(),
            o.PaymentStatus.ToString(),
            o.TotalAmount,
            o.DiscountAmount,
            o.FinalAmount,
            new AddressDto(o.ShippingAddress.Street, o.ShippingAddress.City, o.ShippingAddress.State, o.ShippingAddress.PostalCode, o.ShippingAddress.Country, o.ShippingAddress.RecipientName, o.ShippingAddress.PhoneNumber),
            o.Items.Select(i => new OrderItemDetailDto(i.ProductId, i.ProductName, i.Sku, i.UnitPrice, i.Quantity, i.TotalPrice, i.ImageUrl)).ToList(),
            o.PaymentTransactionId,
            o.CancellationReason,
            o.CreatedAt,
            o.PaidAt
        );
    }
}

public record UpdateOrderStatusCommand(Guid OrderId, OrderStatus NewStatus) : IRequest<Result>;

public class UpdateOrderStatusCommandHandler : IRequestHandler<UpdateOrderStatusCommand, Result>
{
    private readonly IOrderRepository _orderRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;

    public UpdateOrderStatusCommandHandler(IOrderRepository orderRepo, IUnitOfWork unitOfWork, IEventPublisher eventPublisher)
    {
        _orderRepo = orderRepo;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepo.GetByIdAsync(request.OrderId, cancellationToken);
        if (order == null) return Result.Failure(new Error("Order.NotFound", "Order not found."));

        var prevStatus = order.Status.ToString();
        order.UpdateStatus(request.NewStatus);
        await _unitOfWork.CommitChangesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new OrderStatusChangedIntegrationEvent(
            order.Id,
            order.OrderNumber,
            prevStatus,
            order.Status.ToString(),
            DateTimeOffset.UtcNow
        ), cancellationToken);

        return Result.Success();
    }
}
