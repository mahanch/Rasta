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
[Route("api/v1/admin/inventory")]
[Authorize]
[Produces("application/json")]
public class AdminInventoryController : ControllerBase
{
    private readonly ShopDbContext _db;
    private readonly IAuditLogService _auditLogService;

    public AdminInventoryController(ShopDbContext db, IAuditLogService auditLogService)
    {
        _db = db;
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// دریافت ماتریس جامع موجودی انبار کفش تفکیک شده بر اساس سایزها (۳۹ تا ۴۵)
    /// </summary>
    [HttpGet("matrix")]
    [RequirePermission("inventory.manage")]
    public async Task<IActionResult> GetInventoryMatrix(
        [FromQuery] string? search = null,
        [FromQuery] string? alertFilter = null,
        CancellationToken ct = default)
    {
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

        var products = await query.ToListAsync(ct);

        if (!string.IsNullOrWhiteSpace(alertFilter))
        {
            products = alertFilter switch
            {
                "low_stock" => products.Where(p => p.Variants.Any(v => v.Stock > 0 && v.Stock <= v.LowStockThreshold)).ToList(),
                "out_of_stock" => products.Where(p => p.Variants.Any(v => v.Stock == 0)).ToList(),
                "fast_selling" => products.Where(p => p.TotalStock > 15).ToList(),
                "dead_stock" => products.Where(p => p.TotalStock > 0 && p.Variants.All(v => v.Stock >= 8)).ToList(),
                _ => products
            };
        }

        var matrixItems = products.Select(p => new InventoryMatrixItemDto(
            ProductId: p.Id.ToString(),
            PersianName: p.PersianName,
            Name: p.Name,
            Sku: p.Sku,
            CategoryName: p.CategoryName,
            TotalStock: p.TotalStock,
            StockStatus: p.StockStatus,
            Variants: p.Variants
                .OrderBy(v => v.Size)
                .Select(v => new InventoryMatrixVariantDto(
                    VariantId: v.Id.ToString(),
                    Size: v.Size,
                    ColorName: v.ColorName,
                    ColorHex: v.ColorHex,
                    Sku: v.Sku,
                    Stock: v.Stock,
                    LowStockThreshold: v.LowStockThreshold,
                    Status: v.StockStatus
                )).ToList()
        )).ToList();

        return Ok(ApiResponse<List<InventoryMatrixItemDto>>.Ok(matrixItems, "ماتریس موجودی انبار با موفقیت دریافت شد."));
    }

    /// <summary>
    /// اصلاح لحظه‌ای موجودی یک متغیر سایز کفش همراه با ثبت در Audit Log
    /// </summary>
    [HttpPatch("products/{productId}/variants/{variantId}/stock")]
    [RequirePermission("inventory.manage")]
    public async Task<IActionResult> UpdateVariantStock(
        string productId,
        string variantId,
        [FromBody] UpdateVariantStockRequest request,
        CancellationToken ct)
    {
        if (!Guid.TryParse(productId, out var pGuid) || !Guid.TryParse(variantId, out var vGuid))
        {
            return BadRequest(new ApiErrorResponse(400, StandardErrorCodes.ValidationFailed, "شناسه محصول یا سایز نامعتبر است."));
        }

        var product = await _db.FootwearProducts
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == pGuid, ct);

        if (product == null)
        {
            return NotFound(new ApiErrorResponse(404, StandardErrorCodes.NotFound, "محصول یافت نشد."));
        }

        var variant = product.Variants.FirstOrDefault(v => v.Id == vGuid);
        if (variant == null)
        {
            return NotFound(new ApiErrorResponse(404, StandardErrorCodes.NotFound, "متغیر سایز یافت نشد."));
        }

        var oldStock = variant.Stock;
        variant.UpdateStock(request.NewStock);
        await _db.SaveChangesAsync(ct);

        // Record Audit Log automatically
        await _auditLogService.LogAsync(
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "admin",
            User.FindFirstValue(ClaimTypes.Name) ?? "Admin",
            User.FindFirstValue(ClaimTypes.Role) ?? "super_admin",
            "اصلاح موجودی سایز انبار",
            $"{product.PersianName} - سایز {variant.Size} ({variant.Sku})",
            $"{oldStock} جفت",
            $"{request.NewStock} جفت (علت: {request.Reason})",
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            ct
        );

        var dto = new InventoryMatrixVariantDto(
            VariantId: variant.Id.ToString(),
            Size: variant.Size,
            ColorName: variant.ColorName,
            ColorHex: variant.ColorHex,
            Sku: variant.Sku,
            Stock: variant.Stock,
            LowStockThreshold: variant.LowStockThreshold,
            Status: variant.StockStatus
        );

        return Ok(ApiResponse<InventoryMatrixVariantDto>.Ok(dto, "موجودی با موفقیت اصلاح شد و لاگ تغییرات ثبت گردید."));
    }

    /// <summary>
    /// تنظیم آستانه هشدار کم‌موجودی انبار
    /// </summary>
    [HttpPut("settings/threshold")]
    [RequirePermission("inventory.manage")]
    public async Task<IActionResult> UpdateThreshold([FromBody] UpdateThresholdRequest request, CancellationToken ct)
    {
        var setting = await _db.StoreSettings.FirstOrDefaultAsync(ct);
        if (setting == null)
        {
            setting = new StoreSetting();
            _db.StoreSettings.Add(setting);
        }

        setting.GlobalLowStockThreshold = request.LowStockThreshold;

        // Also update variants threshold
        var variants = await _db.FootwearVariants.ToListAsync(ct);
        foreach (var v in variants)
        {
            v.UpdateDetails(v.ColorName, v.ColorHex, v.Sku, request.LowStockThreshold);
        }

        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(null, $"آستانه هشدار کم‌موجودی به {request.LowStockThreshold} تغییر یافت."));
    }
}
