using MediatR;
using Shop.Application.DTOs;
using Shop.Domain.Common;
using Shop.Domain.Entities;
using Shop.Domain.Repositories;

namespace Shop.Application.Features.Cart.Commands;

public record AddItemToCartCommand(
    Guid UserId,
    Guid ProductId,
    int Quantity
) : IRequest<Result<CartDto>>;

public class AddItemToCartCommandHandler : IRequestHandler<AddItemToCartCommand, Result<CartDto>>
{
    private readonly ICartRepository _cartRepo;
    private readonly IProductRepository _productRepo;
    private readonly IUnitOfWork _unitOfWork;

    public AddItemToCartCommandHandler(ICartRepository cartRepo, IProductRepository productRepo, IUnitOfWork unitOfWork)
    {
        _cartRepo = cartRepo;
        _productRepo = productRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CartDto>> Handle(AddItemToCartCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepo.GetByIdAsync(request.ProductId, cancellationToken);
        if (product == null || !product.IsActive)
        {
            return Result<CartDto>.Failure(new Error("Cart.ProductNotFound", "Product is not available."));
        }

        if (product.StockQuantity < request.Quantity)
        {
            return Result<CartDto>.Failure(new Error("Cart.InsufficientStock", $"Only {product.StockQuantity} units available in stock."));
        }

        var cart = await _cartRepo.GetByUserIdAsync(request.UserId, cancellationToken);
        if (cart == null)
        {
            cart = new Domain.Entities.Cart(request.UserId);
            await _cartRepo.AddAsync(cart, cancellationToken);
        }

        var primaryImage = product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl ?? product.Images.FirstOrDefault()?.ImageUrl;
        cart.AddItem(product.Id, product.Name, product.Sku.Value, product.GetCurrentEffectivePrice(), request.Quantity, primaryImage);

        await _unitOfWork.CommitChangesAsync(cancellationToken);

        return Result<CartDto>.Success(MapToDto(cart));
    }

    private static CartDto MapToDto(Domain.Entities.Cart cart)
    {
        return new CartDto(
            cart.UserId,
            cart.Items.Select(i => new CartItemDto(i.ProductId, i.ProductName, i.Sku, i.UnitPrice, i.Quantity, i.TotalPrice, i.ImageUrl)).ToList(),
            cart.TotalAmount,
            cart.TotalQuantity
        );
    }
}

public record UpdateCartItemQuantityCommand(Guid UserId, Guid ProductId, int Quantity) : IRequest<Result<CartDto>>;

public class UpdateCartItemQuantityCommandHandler : IRequestHandler<UpdateCartItemQuantityCommand, Result<CartDto>>
{
    private readonly ICartRepository _cartRepo;
    private readonly IProductRepository _productRepo;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCartItemQuantityCommandHandler(ICartRepository cartRepo, IProductRepository productRepo, IUnitOfWork unitOfWork)
    {
        _cartRepo = cartRepo;
        _productRepo = productRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CartDto>> Handle(UpdateCartItemQuantityCommand request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepo.GetByUserIdAsync(request.UserId, cancellationToken);
        if (cart == null)
        {
            return Result<CartDto>.Failure(new Error("Cart.NotFound", "Cart not found."));
        }

        if (request.Quantity > 0)
        {
            var product = await _productRepo.GetByIdAsync(request.ProductId, cancellationToken);
            if (product != null && product.StockQuantity < request.Quantity)
            {
                return Result<CartDto>.Failure(new Error("Cart.InsufficientStock", $"Only {product.StockQuantity} units available."));
            }
        }

        cart.UpdateItemQuantity(request.ProductId, request.Quantity);
        await _unitOfWork.CommitChangesAsync(cancellationToken);

        return Result<CartDto>.Success(new CartDto(
            cart.UserId,
            cart.Items.Select(i => new CartItemDto(i.ProductId, i.ProductName, i.Sku, i.UnitPrice, i.Quantity, i.TotalPrice, i.ImageUrl)).ToList(),
            cart.TotalAmount,
            cart.TotalQuantity
        ));
    }
}

public record RemoveCartItemCommand(Guid UserId, Guid ProductId) : IRequest<Result<CartDto>>;

public class RemoveCartItemCommandHandler : IRequestHandler<RemoveCartItemCommand, Result<CartDto>>
{
    private readonly ICartRepository _cartRepo;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveCartItemCommandHandler(ICartRepository cartRepo, IUnitOfWork unitOfWork)
    {
        _cartRepo = cartRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CartDto>> Handle(RemoveCartItemCommand request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepo.GetByUserIdAsync(request.UserId, cancellationToken);
        if (cart == null) return Result<CartDto>.Failure(new Error("Cart.NotFound", "Cart not found."));

        cart.RemoveItem(request.ProductId);
        await _unitOfWork.CommitChangesAsync(cancellationToken);

        return Result<CartDto>.Success(new CartDto(
            cart.UserId,
            cart.Items.Select(i => new CartItemDto(i.ProductId, i.ProductName, i.Sku, i.UnitPrice, i.Quantity, i.TotalPrice, i.ImageUrl)).ToList(),
            cart.TotalAmount,
            cart.TotalQuantity
        ));
    }
}

public record ClearCartCommand(Guid UserId) : IRequest<Result>;

public class ClearCartCommandHandler : IRequestHandler<ClearCartCommand, Result>
{
    private readonly ICartRepository _cartRepo;
    private readonly IUnitOfWork _unitOfWork;

    public ClearCartCommandHandler(ICartRepository cartRepo, IUnitOfWork unitOfWork)
    {
        _cartRepo = cartRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepo.GetByUserIdAsync(request.UserId, cancellationToken);
        if (cart != null)
        {
            cart.Clear();
            await _unitOfWork.CommitChangesAsync(cancellationToken);
        }
        return Result.Success();
    }
}
