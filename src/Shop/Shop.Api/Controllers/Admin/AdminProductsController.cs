using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Filters;
using Shop.Application.Common.Interfaces;
using Shop.Application.Common.Models;
using Shop.Application.DTOs;
using Shop.Domain.Entities;
using Shop.Infrastructure.Persistence;

namespace Shop.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/products")]
[Authorize]
[Produces("application/json")]
public class AdminProductsController : ControllerBase
{
    private readonly ShopDbContext _db;
    private readonly IAuditLogService _auditLogService;

    public AdminProductsController(ShopDbContext db, IAuditLogService auditLogService)
    {
        _db = db;
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// فهرست محصولات کفش همراه با فیلتر، جستجو، وضعیت موجودی و صفحه‌بندی
    /// </summary>
    [HttpGet]
    [RequirePermission("products.read")]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] string? stockStatus = "all",
        [FromQuery] string? sortBy = "newest",
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        limit = Math.Max(1, limit);

        var query = _db.FootwearProducts
            .Include(p => p.Variants)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(s) ||
                                     p.PersianName.ToLower().Contains(s) ||
                                     p.Sku.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(category) && category != "all")
        {
            var cat = category.Trim().ToLower();
            query = query.Where(p => p.Category.ToLower() == cat);
        }

        var products = await query.ToListAsync(ct);

        // Filter in-memory for custom stockStatus
        if (!string.IsNullOrWhiteSpace(stockStatus) && stockStatus != "all")
        {
            products = stockStatus switch
            {
                "in_stock" => products.Where(p => p.StockStatus == "in_stock").ToList(),
                "low_stock" => products.Where(p => p.StockStatus == "low_stock").ToList(),
                "out_of_stock" => products.Where(p => p.StockStatus == "out_of_stock").ToList(),
                _ => products
            };
        }

        // Sorting
        products = sortBy switch
        {
            "price_asc" => products.OrderBy(p => p.DiscountPrice ?? p.BasePrice).ToList(),
            "price_desc" => products.OrderByDescending(p => p.DiscountPrice ?? p.BasePrice).ToList(),
            "stock" => products.OrderByDescending(p => p.TotalStock).ToList(),
            _ => products.OrderByDescending(p => p.CreatedAt).ToList()
        };

        var total = products.Count;
        var pagedItems = products
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(MapToDto)
            .ToList();

        var meta = new PaginationMeta(page, limit, total);
        return Ok(ApiResponse<List<FootwearProductDto>>.Ok(pagedItems, "لیست محصولات با موفقیت دریافت شد.", meta));
    }

    /// <summary>
    /// دریافت جزئیات یک محصول با شناسه یکتا یا نامک سئو (slug)
    /// </summary>
    [HttpGet("{id}")]
    [RequirePermission("products.read")]
    public async Task<IActionResult> GetByIdOrSlug(string id, CancellationToken ct)
    {
        FootwearProduct? product;
        if (Guid.TryParse(id, out var guid))
        {
            product = await _db.FootwearProducts
                .Include(p => p.Variants)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == guid, ct);
        }
        else
        {
            product = await _db.FootwearProducts
                .Include(p => p.Variants)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Slug == id.ToLowerInvariant(), ct);
        }

        if (product == null)
        {
            return NotFound(new ApiErrorResponse(404, StandardErrorCodes.NotFound, "محصول مورد نظر یافت نشد."));
        }

        return Ok(ApiResponse<FootwearProductDto>.Ok(MapToDto(product)));
    }

    /// <summary>
    /// افزودن محصول کفش دست‌دوز جدید به کاتالوگ با سایزبندی ۳۹ تا ۴۵
    /// </summary>
    [HttpPost]
    [RequirePermission("products.create")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateFootwearProductRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Sku))
        {
            return BadRequest(new ApiErrorResponse(
                400,
                StandardErrorCodes.ValidationFailed,
                "نام و کد انبارداری (SKU) الزامی است."
            ));
        }

        var normalizedSku = request.Sku.Trim();
        if (await _db.FootwearProducts.AnyAsync(p => p.Sku == normalizedSku, ct))
        {
            return Conflict(new ApiErrorResponse(
                409,
                StandardErrorCodes.SkuAlreadyExists,
                "کد انبارداری (SKU) وارد شده قبلاً برای کفش دیگری ثبت شده است.",
                [new ValidationErrorDetail("sku", "کد انبارداری (SKU) وارد شده تکراری است.")]
            ));
        }

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? request.Name.Trim().ToLower().Replace(' ', '-')
            : request.Slug.Trim().ToLower();

        var variants = request.Variants?.Select(v => new FootwearVariant(
            v.Size,
            v.ColorName,
            v.ColorHex,
            v.Sku,
            v.Stock,
            v.LowStockThreshold
        )).ToList() ?? [];

        // If no variants provided, default sizes 39 to 45
        if (variants.Count == 0)
        {
            for (int s = 39; s <= 45; s++)
            {
                variants.Add(new FootwearVariant(s, "قهوه‌ای تیره", "#3B2314", $"{normalizedSku}-{s}", 5, 3));
            }
        }

        var product = new FootwearProduct(
            persianName: request.PersianName ?? request.Name,
            name: request.Name,
            slug: slug,
            sku: normalizedSku,
            category: request.Category ?? "formal",
            categoryName: request.CategoryName ?? "کفش رسمی",
            collection: request.Collection ?? "کفش‌های دست‌دوز شاهکار",
            gender: request.Gender ?? "men",
            basePrice: request.BasePrice,
            discountPrice: request.DiscountPrice,
            costPrice: request.CostPrice,
            specs: new FootwearSpecs
            {
                Material = request.Specs?.Material ?? "",
                LeatherType = request.Specs?.LeatherType ?? "",
                Tannery = request.Specs?.Tannery ?? "",
                SoleMaterial = request.Specs?.SoleMaterial ?? "",
                Construction = request.Specs?.Construction ?? "",
                Origin = request.Specs?.Origin ?? ""
            },
            seo: new FootwearSeo
            {
                Title = request.Seo?.Title ?? "",
                MetaDescription = request.Seo?.MetaDescription ?? "",
                CanonicalUrl = request.Seo?.CanonicalUrl ?? "",
                FocusKeyword = request.Seo?.FocusKeyword ?? ""
            },
            variants: variants,
            images: request.Images ?? [],
            shortDescription: request.ShortDescription ?? "",
            fullDescription: request.FullDescription ?? "",
            careInstructions: request.CareInstructions ?? [],
            status: request.Status ?? "published"
        );

        _db.FootwearProducts.Add(product);
        await _db.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "admin",
            User.FindFirstValue(ClaimTypes.Name) ?? "Admin",
            User.FindFirstValue(ClaimTypes.Role) ?? "super_admin",
            "ایجاد محصول جدید",
            $"{product.PersianName} ({product.Sku})",
            "-",
            $"قیمت پایه: {product.BasePrice:N0} تومان",
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            ct
        );

        var dto = MapToDto(product);
        return StatusCode(201, ApiResponse<FootwearProductDto>.Created(dto, "محصول جدید با موفقیت ایجاد شد."));
    }

    /// <summary>
    /// ویرایش اطلاعات کفش دست‌دوز
    /// </summary>
    [HttpPut("{id}")]
    [RequirePermission("products.update")]
    public async Task<IActionResult> UpdateProduct(
        string id,
        [FromBody] UpdateFootwearProductRequest request,
        CancellationToken ct)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            return BadRequest(new ApiErrorResponse(400, StandardErrorCodes.ValidationFailed, "شناسه محصول نامعتبر است."));
        }

        var product = await _db.FootwearProducts
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == guid, ct);

        if (product == null)
        {
            return NotFound(new ApiErrorResponse(404, StandardErrorCodes.NotFound, "محصول یافت نشد."));
        }

        var oldPrice = product.BasePrice;
        product.Update(
            request.PersianName,
            request.Name,
            request.Slug,
            request.Sku,
            request.Category,
            request.CategoryName,
            request.Collection,
            request.Gender,
            request.BasePrice,
            request.DiscountPrice,
            request.CostPrice,
            new FootwearSpecs
            {
                Material = request.Specs?.Material ?? "",
                LeatherType = request.Specs?.LeatherType ?? "",
                Tannery = request.Specs?.Tannery ?? "",
                SoleMaterial = request.Specs?.SoleMaterial ?? "",
                Construction = request.Specs?.Construction ?? "",
                Origin = request.Specs?.Origin ?? ""
            },
            new FootwearSeo
            {
                Title = request.Seo?.Title ?? "",
                MetaDescription = request.Seo?.MetaDescription ?? "",
                CanonicalUrl = request.Seo?.CanonicalUrl ?? "",
                FocusKeyword = request.Seo?.FocusKeyword ?? ""
            },
            request.Images ?? product.Images,
            request.ShortDescription,
            request.FullDescription,
            request.CareInstructions ?? product.CareInstructions,
            request.Status
        );

        if (request.Variants != null && request.Variants.Count > 0)
        {
            var newVariants = request.Variants.Select(v => new FootwearVariant(
                v.Size,
                v.ColorName,
                v.ColorHex,
                v.Sku,
                v.Stock,
                v.LowStockThreshold
            )).ToList();
            product.ReplaceVariants(newVariants);
        }

        await _db.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "admin",
            User.FindFirstValue(ClaimTypes.Name) ?? "Admin",
            User.FindFirstValue(ClaimTypes.Role) ?? "super_admin",
            "ویرایش مشخصات و قیمت محصول",
            $"{product.PersianName} ({product.Sku})",
            $"{oldPrice:N0} تومان",
            $"{product.BasePrice:N0} تومان",
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            ct
        );

        return Ok(ApiResponse<FootwearProductDto>.Ok(MapToDto(product), "محصول با موفقیت ویرایش شد."));
    }

    /// <summary>
    /// حذف محصول کفش از کاتالوگ
    /// </summary>
    [HttpDelete("{id}")]
    [RequirePermission("products.delete")]
    public async Task<IActionResult> DeleteProduct(string id, CancellationToken ct)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            return BadRequest(new ApiErrorResponse(400, StandardErrorCodes.ValidationFailed, "شناسه نامعتبر است."));
        }

        var product = await _db.FootwearProducts
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == guid, ct);

        if (product == null)
        {
            return NotFound(new ApiErrorResponse(404, StandardErrorCodes.NotFound, "محصول یافت نشد."));
        }

        _db.FootwearProducts.Remove(product);
        await _db.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "admin",
            User.FindFirstValue(ClaimTypes.Name) ?? "Admin",
            User.FindFirstValue(ClaimTypes.Role) ?? "super_admin",
            "حذف محصول از کاتالوگ",
            $"{product.PersianName} ({product.Sku})",
            "فعال در کاتالوگ",
            "حذف شده",
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            ct
        );

        return Ok(ApiResponse<object>.Ok(null, "محصول با موفقیت حذف شد."));
    }

    /// <summary>
    /// عملیات گروهی کاتالوگ (انتشار، پیش‌نویس، حذف)
    /// </summary>
    [HttpPost("bulk")]
    [RequirePermission("products.update")]
    public async Task<IActionResult> BulkAction([FromBody] BulkProductActionRequest request, CancellationToken ct)
    {
        var guids = request.ProductIds
            .Select(id => Guid.TryParse(id, out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .ToList();

        var products = await _db.FootwearProducts.Where(p => guids.Contains(p.Id)).ToListAsync(ct);

        switch (request.Action.ToLowerInvariant())
        {
            case "publish":
                foreach (var p in products) p.SetStatus("published");
                break;
            case "draft":
                foreach (var p in products) p.SetStatus("draft");
                break;
            case "delete":
                _db.FootwearProducts.RemoveRange(products);
                break;
            default:
                return BadRequest(new ApiErrorResponse(400, StandardErrorCodes.ValidationFailed, "عملیات نامعتبر است."));
        }

        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(null, $"عملیات گروهی '{request.Action}' روی {products.Count} محصول اعمال گردید."));
    }

    private static FootwearProductDto MapToDto(FootwearProduct p)
    {
        return new FootwearProductDto(
            Id: p.Id.ToString(),
            PersianName: p.PersianName,
            Name: p.Name,
            Slug: p.Slug,
            Sku: p.Sku,
            Category: p.Category,
            CategoryName: p.CategoryName,
            Collection: p.Collection,
            Gender: p.Gender,
            BasePrice: p.BasePrice,
            DiscountPrice: p.DiscountPrice,
            CostPrice: p.CostPrice,
            Specs: new FootwearSpecsDto(
                p.Specs.Material,
                p.Specs.LeatherType,
                p.Specs.Tannery,
                p.Specs.SoleMaterial,
                p.Specs.Construction,
                p.Specs.Origin
            ),
            Variants: p.Variants.Select(v => new FootwearVariantDto(
                v.Id.ToString(),
                v.Size,
                v.ColorName,
                v.ColorHex,
                v.Sku,
                v.Stock,
                v.LowStockThreshold,
                v.StockStatus
            )).OrderBy(v => v.Size).ToList(),
            Images: p.Images,
            ShortDescription: p.ShortDescription,
            FullDescription: p.FullDescription,
            CareInstructions: p.CareInstructions,
            Status: p.Status,
            Seo: new FootwearSeoDto(
                p.Seo.Title,
                p.Seo.MetaDescription,
                p.Seo.CanonicalUrl,
                p.Seo.FocusKeyword
            ),
            TotalStock: p.TotalStock,
            StockStatus: p.StockStatus,
            CreatedAt: p.CreatedAt.ToString("yyyy/MM/dd - HH:mm")
        );
    }
}
