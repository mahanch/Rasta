using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Filters;
using Shop.Application.Common.Models;
using Shop.Application.DTOs;
using Shop.Domain.Entities;
using Shop.Infrastructure.Persistence;

namespace Shop.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin")]
[Authorize]
[Produces("application/json")]
public class AdminMarketingController : ControllerBase
{
    private readonly ShopDbContext _db;

    public AdminMarketingController(ShopDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// لیست کدهای تخفیف درصدی و ریالی
    /// </summary>
    [HttpGet("coupons")]
    [RequirePermission("marketing.read")]
    public async Task<IActionResult> GetCoupons(CancellationToken ct)
    {
        var list = await _db.Coupons.AsNoTracking().OrderByDescending(c => c.CreatedAt).ToListAsync(ct);
        var dtos = list.Select(c => new CouponDto(
            c.Id.ToString(),
            c.Code,
            c.Type,
            c.Value,
            c.MaxDiscount,
            c.MinCartSpend,
            c.StartDate,
            c.EndDate,
            c.TotalLimit,
            c.UsedCount,
            c.PerUserLimit,
            c.TargetTiers,
            c.IsActive
        )).ToList();

        return Ok(ApiResponse<List<CouponDto>>.Ok(dtos));
    }

    /// <summary>
    /// ایجاد کوپن تخفیف جدید
    /// </summary>
    [HttpPost("coupons")]
    [RequirePermission("marketing.create")]
    public async Task<IActionResult> CreateCoupon([FromBody] CreateCouponRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || request.Value <= 0)
        {
            return BadRequest(new ApiErrorResponse(400, StandardErrorCodes.ValidationFailed, "کد تخفیف و مقدار آن الزامی است."));
        }

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        if (await _db.Coupons.AnyAsync(c => c.Code == normalizedCode, ct))
        {
            return Conflict(new ApiErrorResponse(409, StandardErrorCodes.Conflict, "کد تخفیف وارد شده قبلاً تعریف شده است."));
        }

        var coupon = new Coupon(
            normalizedCode,
            request.Type,
            request.Value,
            request.MaxDiscount,
            request.MinCartSpend,
            request.StartDate,
            request.EndDate,
            request.TotalLimit,
            request.PerUserLimit,
            request.TargetTiers
        );

        _db.Coupons.Add(coupon);
        await _db.SaveChangesAsync(ct);

        var dto = new CouponDto(
            coupon.Id.ToString(),
            coupon.Code,
            coupon.Type,
            coupon.Value,
            coupon.MaxDiscount,
            coupon.MinCartSpend,
            coupon.StartDate,
            coupon.EndDate,
            coupon.TotalLimit,
            coupon.UsedCount,
            coupon.PerUserLimit,
            coupon.TargetTiers,
            coupon.IsActive
        );

        return StatusCode(201, ApiResponse<CouponDto>.Created(dto, "کوپن تخفیف با موفقیت ایجاد شد."));
    }

    /// <summary>
    /// فعال یا غیرفعال کردن کوپن تخفیف
    /// </summary>
    [HttpPatch("coupons/{id}/toggle")]
    [RequirePermission("marketing.update")]
    public async Task<IActionResult> ToggleCoupon(string id, CancellationToken ct)
    {
        Coupon? coupon;
        if (Guid.TryParse(id, out var guid))
        {
            coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Id == guid, ct);
        }
        else
        {
            coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Code == id.ToUpperInvariant(), ct);
        }

        if (coupon == null)
        {
            return NotFound(new ApiErrorResponse(404, StandardErrorCodes.NotFound, "کوپن تخفیف یافت نشد."));
        }

        coupon.Toggle();
        await _db.SaveChangesAsync(ct);

        return Ok(ApiResponse<object>.Ok(new { coupon.Id, coupon.IsActive }, $"وضعیت کوپن به {(coupon.IsActive ? "فعال" : "غیرفعال")} تغییر یافت."));
    }

    /// <summary>
    /// لیست کمپین‌های فصلی و آمار عملکرد (کلیک، سفارش، درآمد و نرخ بازگشت سرمایه ROI)
    /// </summary>
    [HttpGet("campaigns")]
    [RequirePermission("marketing.read")]
    public async Task<IActionResult> GetCampaigns(CancellationToken ct)
    {
        var list = await _db.Campaigns.AsNoTracking().OrderByDescending(c => c.CreatedAt).ToListAsync(ct);
        var dtos = list.Select(c => new CampaignDto(
            c.Id.ToString(),
            c.Title,
            c.StartDate,
            c.EndDate,
            c.ClicksCount,
            c.OrdersCount,
            c.Revenue,
            c.SpendCost,
            c.Roi,
            c.IsActive
        )).ToList();

        return Ok(ApiResponse<List<CampaignDto>>.Ok(dtos));
    }
}
