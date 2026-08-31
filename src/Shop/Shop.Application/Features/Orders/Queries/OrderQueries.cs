using MediatR;
using MongoDB.Driver;
using Shop.Application.Common.Interfaces;
using Shop.Application.DTOs;
using Shop.Application.ReadModels;
using Shop.Domain.Common;

namespace Shop.Application.Features.Orders.Queries;

public record GetOrderByIdQuery(Guid OrderId, Guid? UserId = null) : IRequest<Result<OrderDto>>;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    private readonly IMongoReadDbContext _mongo;

    public GetOrderByIdQueryHandler(IMongoReadDbContext mongo)
    {
        _mongo = mongo;
    }

    public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var collection = _mongo.GetCollection<OrderReadModel>("orders_view");
        var filter = Builders<OrderReadModel>.Filter.Eq(o => o.Id, request.OrderId);
        if (request.UserId.HasValue)
        {
            filter &= Builders<OrderReadModel>.Filter.Eq(o => o.UserId, request.UserId.Value);
        }

        var order = await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        if (order == null)
        {
            return Result<OrderDto>.Failure(new Error("Order.NotFound", "Order not found."));
        }

        return Result<OrderDto>.Success(new OrderDto(
            order.Id,
            order.OrderNumber,
            order.UserId,
            order.Status,
            order.PaymentStatus,
            order.TotalAmount,
            order.DiscountAmount,
            order.FinalAmount,
            new AddressDto(order.ShippingAddress.Street, order.ShippingAddress.City, order.ShippingAddress.State, order.ShippingAddress.PostalCode, order.ShippingAddress.Country, order.ShippingAddress.RecipientName, order.ShippingAddress.PhoneNumber),
            order.Items.Select(i => new OrderItemDetailDto(i.ProductId, i.ProductName, i.Sku, i.UnitPrice, i.Quantity, i.TotalPrice, i.ImageUrl)).ToList(),
            order.PaymentTransactionId,
            order.CancellationReason,
            order.CreatedAt,
            order.PaidAt
        ));
    }
}

public record GetCustomerOrdersQuery(Guid UserId, int Page = 1, int PageSize = 10) : IRequest<PaginatedResult<OrderDto>>;

public class GetCustomerOrdersQueryHandler : IRequestHandler<GetCustomerOrdersQuery, PaginatedResult<OrderDto>>
{
    private readonly IMongoReadDbContext _mongo;

    public GetCustomerOrdersQueryHandler(IMongoReadDbContext mongo)
    {
        _mongo = mongo;
    }

    public async Task<PaginatedResult<OrderDto>> Handle(GetCustomerOrdersQuery request, CancellationToken cancellationToken)
    {
        var collection = _mongo.GetCollection<OrderReadModel>("orders_view");
        var filter = Builders<OrderReadModel>.Filter.Eq(o => o.UserId, request.UserId);

        var total = await collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        var skip = Math.Max(0, (request.Page - 1) * request.PageSize);
        var orders = await collection.Find(filter).SortByDescending(o => o.CreatedAt).Skip(skip).Limit(request.PageSize).ToListAsync(cancellationToken);

        var dtos = orders.Select(o => new OrderDto(
            o.Id,
            o.OrderNumber,
            o.UserId,
            o.Status,
            o.PaymentStatus,
            o.TotalAmount,
            o.DiscountAmount,
            o.FinalAmount,
            new AddressDto(o.ShippingAddress.Street, o.ShippingAddress.City, o.ShippingAddress.State, o.ShippingAddress.PostalCode, o.ShippingAddress.Country, o.ShippingAddress.RecipientName, o.ShippingAddress.PhoneNumber),
            o.Items.Select(i => new OrderItemDetailDto(i.ProductId, i.ProductName, i.Sku, i.UnitPrice, i.Quantity, i.TotalPrice, i.ImageUrl)).ToList(),
            o.PaymentTransactionId,
            o.CancellationReason,
            o.CreatedAt,
            o.PaidAt
        )).ToList();

        var totalPages = (int)Math.Ceiling((double)total / request.PageSize);
        return new PaginatedResult<OrderDto>(dtos, (int)total, request.Page, request.PageSize, totalPages);
    }
}

public record GetAdminOrdersQuery(string? Status = null, int Page = 1, int PageSize = 20) : IRequest<PaginatedResult<OrderDto>>;

public class GetAdminOrdersQueryHandler : IRequestHandler<GetAdminOrdersQuery, PaginatedResult<OrderDto>>
{
    private readonly IMongoReadDbContext _mongo;

    public GetAdminOrdersQueryHandler(IMongoReadDbContext mongo)
    {
        _mongo = mongo;
    }

    public async Task<PaginatedResult<OrderDto>> Handle(GetAdminOrdersQuery request, CancellationToken cancellationToken)
    {
        var collection = _mongo.GetCollection<OrderReadModel>("orders_view");
        var filter = Builders<OrderReadModel>.Filter.Empty;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            filter &= Builders<OrderReadModel>.Filter.Eq(o => o.Status, request.Status);
        }

        var total = await collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        var skip = Math.Max(0, (request.Page - 1) * request.PageSize);
        var orders = await collection.Find(filter).SortByDescending(o => o.CreatedAt).Skip(skip).Limit(request.PageSize).ToListAsync(cancellationToken);

        var dtos = orders.Select(o => new OrderDto(
            o.Id,
            o.OrderNumber,
            o.UserId,
            o.Status,
            o.PaymentStatus,
            o.TotalAmount,
            o.DiscountAmount,
            o.FinalAmount,
            new AddressDto(o.ShippingAddress.Street, o.ShippingAddress.City, o.ShippingAddress.State, o.ShippingAddress.PostalCode, o.ShippingAddress.Country, o.ShippingAddress.RecipientName, o.ShippingAddress.PhoneNumber),
            o.Items.Select(i => new OrderItemDetailDto(i.ProductId, i.ProductName, i.Sku, i.UnitPrice, i.Quantity, i.TotalPrice, i.ImageUrl)).ToList(),
            o.PaymentTransactionId,
            o.CancellationReason,
            o.CreatedAt,
            o.PaidAt
        )).ToList();

        var totalPages = (int)Math.Ceiling((double)total / request.PageSize);
        return new PaginatedResult<OrderDto>(dtos, (int)total, request.Page, request.PageSize, totalPages);
    }
}
