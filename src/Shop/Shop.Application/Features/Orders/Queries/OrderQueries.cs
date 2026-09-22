using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Application.DTOs;
using Shop.Domain.Common;
using Shop.Domain.Entities;
using Shop.Domain.ValueObjects;

namespace Shop.Application.Features.Orders.Queries;

public record GetOrderByIdQuery(Guid OrderId, Guid? UserId = null) : IRequest<Result<OrderDto>>;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    private readonly IShopDbContext _db;

    public GetOrderByIdQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.Id == request.OrderId);

        if (request.UserId.HasValue)
        {
            query = query.Where(o => o.UserId == request.UserId.Value);
        }

        var order = await query.FirstOrDefaultAsync(cancellationToken);
        if (order == null)
        {
            return Result<OrderDto>.Failure(new Error("Order.NotFound", "Order not found."));
        }

        return Result<OrderDto>.Success(OrderQueryHelpers.MapToDto(order));
    }
}

public record GetCustomerOrdersQuery(Guid UserId, int Page = 1, int PageSize = 10) : IRequest<PaginatedResult<OrderDto>>;

public class GetCustomerOrdersQueryHandler : IRequestHandler<GetCustomerOrdersQuery, PaginatedResult<OrderDto>>
{
    private readonly IShopDbContext _db;

    public GetCustomerOrdersQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<PaginatedResult<OrderDto>> Handle(GetCustomerOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.UserId == request.UserId);

        var total = await query.CountAsync(cancellationToken);
        var skip = Math.Max(0, (request.Page - 1) * request.PageSize);
        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip(skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = orders.Select(OrderQueryHelpers.MapToDto).ToList();
        var totalPages = (int)Math.Ceiling((double)total / request.PageSize);
        return new PaginatedResult<OrderDto>(dtos, total, request.Page, request.PageSize, totalPages);
    }
}

public record GetAdminOrdersQuery(string? Status = null, int Page = 1, int PageSize = 20) : IRequest<PaginatedResult<OrderDto>>;

public class GetAdminOrdersQueryHandler : IRequestHandler<GetAdminOrdersQuery, PaginatedResult<OrderDto>>
{
    private readonly IShopDbContext _db;

    public GetAdminOrdersQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<PaginatedResult<OrderDto>> Handle(GetAdminOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<OrderStatus>(request.Status, true, out var parsedStatus))
        {
            query = query.Where(o => o.Status == parsedStatus);
        }

        var total = await query.CountAsync(cancellationToken);
        var skip = Math.Max(0, (request.Page - 1) * request.PageSize);
        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip(skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = orders.Select(OrderQueryHelpers.MapToDto).ToList();
        var totalPages = (int)Math.Ceiling((double)total / request.PageSize);
        return new PaginatedResult<OrderDto>(dtos, total, request.Page, request.PageSize, totalPages);
    }
}

internal static class OrderQueryHelpers
{
    public static OrderDto MapToDto(Order o) =>
        new(
            o.Id,
            o.OrderNumber,
            o.UserId,
            o.Status.ToString(),
            o.PaymentStatus.ToString(),
            o.TotalAmount,
            o.DiscountAmount,
            o.FinalAmount,
            ToAddressDto(o.ShippingAddress),
            o.Items.Select(i => new OrderItemDetailDto(i.ProductId, i.ProductName, i.Sku, i.UnitPrice, i.Quantity, i.TotalPrice, i.ImageUrl)).ToList(),
            o.PaymentTransactionId,
            o.CancellationReason,
            o.CreatedAt,
            o.PaidAt
        );

    private static AddressDto ToAddressDto(Address? a) =>
        a != null
            ? new AddressDto(a.Street, a.City, a.State, a.PostalCode, a.Country, a.RecipientName, a.PhoneNumber)
            : new AddressDto(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
}
