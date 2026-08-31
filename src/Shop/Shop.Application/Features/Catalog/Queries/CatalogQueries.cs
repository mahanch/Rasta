using MediatR;
using MongoDB.Driver;
using Shop.Application.Common.Interfaces;
using Shop.Application.DTOs;
using Shop.Application.ReadModels;
using Shop.Domain.Common;

namespace Shop.Application.Features.Catalog.Queries;

public record GetProductsQuery(
    string? Search = null,
    Guid? CategoryId = null,
    Guid? BrandId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    bool? InStockOnly = null,
    string? SortBy = "newest",
    int Page = 1,
    int PageSize = 12
) : IRequest<PaginatedResult<ProductDto>>;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PaginatedResult<ProductDto>>
{
    private readonly IMongoReadDbContext _mongo;

    public GetProductsQueryHandler(IMongoReadDbContext mongo)
    {
        _mongo = mongo;
    }

    public async Task<PaginatedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var collection = _mongo.GetCollection<ProductReadModel>("products_view");
        var builder = Builders<ProductReadModel>.Filter;
        var filter = builder.Eq(p => p.IsActive, true);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchRegex = new MongoDB.Bson.BsonRegularExpression(request.Search, "i");
            filter &= (builder.Regex(p => p.Name, searchRegex) | builder.Regex(p => p.Description, searchRegex) | builder.Regex(p => p.Sku, searchRegex));
        }

        if (request.CategoryId.HasValue)
        {
            filter &= builder.Eq(p => p.CategoryId, request.CategoryId.Value);
        }

        if (request.BrandId.HasValue)
        {
            filter &= builder.Eq(p => p.BrandId, request.BrandId.Value);
        }

        if (request.MinPrice.HasValue)
        {
            filter &= builder.Gte(p => p.Price, request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            filter &= builder.Lte(p => p.Price, request.MaxPrice.Value);
        }

        if (request.InStockOnly == true)
        {
            filter &= builder.Gt(p => p.StockQuantity, 0);
        }

        var count = await collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var query = collection.Find(filter);
        query = request.SortBy?.ToLowerInvariant() switch
        {
            "price_asc" => query.SortBy(p => p.Price),
            "price_desc" => query.SortByDescending(p => p.Price),
            _ => query.SortByDescending(p => p.CreatedAt)
        };

        var skip = Math.Max(0, (request.Page - 1) * request.PageSize);
        var items = await query.Skip(skip).Limit(request.PageSize).ToListAsync(cancellationToken);

        var dtos = items.Select(p => new ProductDto(
            p.Id,
            p.Name,
            p.Slug,
            p.Description,
            p.Sku,
            p.Price,
            p.DiscountPrice,
            p.DiscountPrice ?? p.Price,
            p.StockQuantity,
            p.InStock,
            p.IsActive,
            p.CategoryId,
            p.CategoryName,
            p.BrandId,
            p.BrandName,
            p.ImageUrls,
            p.CreatedAt
        )).ToList();

        var totalPages = (int)Math.Ceiling((double)count / request.PageSize);
        return new PaginatedResult<ProductDto>(dtos, (int)count, request.Page, request.PageSize, totalPages);
    }
}

public record GetProductByIdQuery(Guid Id) : IRequest<Result<ProductDto>>;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, Result<ProductDto>>
{
    private readonly IMongoReadDbContext _mongo;

    public GetProductByIdQueryHandler(IMongoReadDbContext mongo)
    {
        _mongo = mongo;
    }

    public async Task<Result<ProductDto>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var collection = _mongo.GetCollection<ProductReadModel>("products_view");
        var p = await collection.Find(x => x.Id == request.Id).FirstOrDefaultAsync(cancellationToken);
        if (p == null) return Result<ProductDto>.Failure(new Error("Product.NotFound", "Product not found."));

        return Result<ProductDto>.Success(new ProductDto(
            p.Id, p.Name, p.Slug, p.Description, p.Sku, p.Price, p.DiscountPrice,
            p.DiscountPrice ?? p.Price, p.StockQuantity, p.InStock, p.IsActive,
            p.CategoryId, p.CategoryName, p.BrandId, p.BrandName, p.ImageUrls, p.CreatedAt
        ));
    }
}

public record GetProductBySlugQuery(string Slug) : IRequest<Result<ProductDto>>;

public class GetProductBySlugQueryHandler : IRequestHandler<GetProductBySlugQuery, Result<ProductDto>>
{
    private readonly IMongoReadDbContext _mongo;

    public GetProductBySlugQueryHandler(IMongoReadDbContext mongo)
    {
        _mongo = mongo;
    }

    public async Task<Result<ProductDto>> Handle(GetProductBySlugQuery request, CancellationToken cancellationToken)
    {
        var collection = _mongo.GetCollection<ProductReadModel>("products_view");
        var p = await collection.Find(x => x.Slug == request.Slug.ToLowerInvariant()).FirstOrDefaultAsync(cancellationToken);
        if (p == null) return Result<ProductDto>.Failure(new Error("Product.NotFound", "Product not found."));

        return Result<ProductDto>.Success(new ProductDto(
            p.Id, p.Name, p.Slug, p.Description, p.Sku, p.Price, p.DiscountPrice,
            p.DiscountPrice ?? p.Price, p.StockQuantity, p.InStock, p.IsActive,
            p.CategoryId, p.CategoryName, p.BrandId, p.BrandName, p.ImageUrls, p.CreatedAt
        ));
    }
}

public record GetCategoriesQuery() : IRequest<List<CategoryDto>>;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, List<CategoryDto>>
{
    private readonly IMongoReadDbContext _mongo;

    public GetCategoriesQueryHandler(IMongoReadDbContext mongo)
    {
        _mongo = mongo;
    }

    public async Task<List<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var collection = _mongo.GetCollection<CategoryReadModel>("categories_view");
        var list = await collection.Find(c => c.IsActive).ToListAsync(cancellationToken);
        return list.Select(c => new CategoryDto(c.Id, c.Name, c.Slug, c.Description, c.ImageUrl, c.IsActive, c.ParentCategoryId)).ToList();
    }
}
