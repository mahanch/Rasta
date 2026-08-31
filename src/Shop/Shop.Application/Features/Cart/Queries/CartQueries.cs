using MediatR;
using Shop.Application.DTOs;
using Shop.Domain.Common;
using Shop.Domain.Repositories;

namespace Shop.Application.Features.Cart.Queries;

public record GetCartQuery(Guid UserId) : IRequest<Result<CartDto>>;

public class GetCartQueryHandler : IRequestHandler<GetCartQuery, Result<CartDto>>
{
    private readonly ICartRepository _cartRepo;

    public GetCartQueryHandler(ICartRepository cartRepo)
    {
        _cartRepo = cartRepo;
    }

    public async Task<Result<CartDto>> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepo.GetByUserIdAsync(request.UserId, cancellationToken);
        if (cart == null)
        {
            return Result<CartDto>.Success(new CartDto(request.UserId, [], 0, 0));
        }

        return Result<CartDto>.Success(new CartDto(
            cart.UserId,
            cart.Items.Select(i => new CartItemDto(i.ProductId, i.ProductName, i.Sku, i.UnitPrice, i.Quantity, i.TotalPrice, i.ImageUrl)).ToList(),
            cart.TotalAmount,
            cart.TotalQuantity
        ));
    }
}
