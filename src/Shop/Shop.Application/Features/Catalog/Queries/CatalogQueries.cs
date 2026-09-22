using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Application.DTOs;
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
    private readonly IShopDbContext _db;

    public GetProductsQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<PaginatedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Images)
            .Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(search) ||
                p.Description.ToLower().Contains(search));
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        }

        if (request.BrandId.HasValue)
        {
            query = query.Where(p => p.BrandId == request.BrandId.Value);
        }

        if (request.MinPrice.HasValue)
        {
            query = query.Where(p => p.Price.Amount >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(p => p.Price.Amount <= request.MaxPrice.Value);
        }

        if (request.InStockOnly == true)
        {
            query = query.Where(p => p.StockQuantity > 0);
        }

        var count = await query.CountAsync(cancellationToken);

        query = request.SortBy?.ToLowerInvariant() switch
        {
            "price_asc" => query.OrderBy(p => p.Price.Amount),
            "price_desc" => query.OrderByDescending(p => p.Price.Amount),
            "name" => query.OrderBy(p => p.Name),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var skip = Math.Max(0, (request.Page - 1) * request.PageSize);
        var items = await query.Skip(skip).Take(request.PageSize).ToListAsync(cancellationToken);

        var dtos = items.Select(p => new ProductDto(
            p.Id,
            p.Name,
            p.Slug,
            p.Description,
            p.Sku.Value,
            p.Price.Amount,
            p.DiscountPrice?.Amount,
            p.GetCurrentEffectivePrice(),
            p.StockQuantity,
            p.StockQuantity > 0,
            p.IsActive,
            p.CategoryId,
            p.Category.Name,
            p.BrandId,
            p.Brand?.Name,
            p.Images.OrderBy(i => i.DisplayOrder).Select(i => i.ImageUrl).ToList(),
            p.CreatedAt
        )).ToList();

        var totalPages = (int)Math.Ceiling((double)count / request.PageSize);
        return new PaginatedResult<ProductDto>(dtos, count, request.Page, request.PageSize, totalPages);
    }
}

public record GetProductByIdQuery(Guid Id) : IRequest<Result<ProductDto>>;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, Result<ProductDto>>
{
    private readonly IShopDbContext _db;

    public GetProductByIdQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<ProductDto>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var p = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (p == null) return Result<ProductDto>.Failure(new Error("Product.NotFound", "Product not found."));

        return Result<ProductDto>.Success(new ProductDto(
            p.Id, p.Name, p.Slug, p.Description, p.Sku.Value, p.Price.Amount, p.DiscountPrice?.Amount,
            p.GetCurrentEffectivePrice(), p.StockQuantity, p.StockQuantity > 0, p.IsActive,
            p.CategoryId, p.Category.Name, p.BrandId, p.Brand?.Name,
            p.Images.OrderBy(i => i.DisplayOrder).Select(i => i.ImageUrl).ToList(), p.CreatedAt
        ));
    }
}

public record GetProductBySlugQuery(string Slug) : IRequest<Result<ProductDto>>;

public class GetProductBySlugQueryHandler : IRequestHandler<GetProductBySlugQuery, Result<ProductDto>>
{
    private readonly IShopDbContext _db;

    public GetProductBySlugQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<ProductDto>> Handle(GetProductBySlugQuery request, CancellationToken cancellationToken)
    {
        var slug = request.Slug.ToLowerInvariant();
        var p = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);

        if (p == null) return Result<ProductDto>.Failure(new Error("Product.NotFound", "Product not found."));

        return Result<ProductDto>.Success(new ProductDto(
            p.Id, p.Name, p.Slug, p.Description, p.Sku.Value, p.Price.Amount, p.DiscountPrice?.Amount,
            p.GetCurrentEffectivePrice(), p.StockQuantity, p.StockQuantity > 0, p.IsActive,
            p.CategoryId, p.Category.Name, p.BrandId, p.Brand?.Name,
            p.Images.OrderBy(i => i.DisplayOrder).Select(i => i.ImageUrl).ToList(), p.CreatedAt
        ));
    }
}

public record GetCategoriesQuery() : IRequest<List<CategoryDto>>;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, List<CategoryDto>>
{
    private readonly IShopDbContext _db;

    public GetCategoriesQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<List<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .ToListAsync(cancellationToken);

        return list.Select(c => new CategoryDto(c.Id, c.Name, c.Slug, c.Description, c.ImageUrl, c.IsActive, c.ParentCategoryId)).ToList();
    }
}
