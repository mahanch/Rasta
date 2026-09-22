using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Filters;
using Shop.Application.Common.Interfaces;
using Shop.Application.Common.Models;
using Shop.Application.DTOs;
using Shop.Infrastructure.Persistence;

namespace Shop.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/abandoned-carts")]
[Authorize]
[Produces("application/json")]
public class AdminAbandonedCartsController : ControllerBase
{
    private readonly ShopDbContext _db;
    private readonly IAuditLogService _auditLogService;

    public AdminAbandonedCartsController(ShopDbContext db, IAuditLogService auditLogService)
    {
        _db = db;
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// فهرست سبدهای خرید رهاشده با توقف بیش از ۲ ساعت
    /// </summary>
    [HttpGet]
    [RequirePermission("marketing.read")]
    public async Task<IActionResult> GetAbandonedCarts(CancellationToken ct)
    {
        var list = await _db.AbandonedCartRecords
            .AsNoTracking()
            .OrderByDescending(c => c.LastActiveAt)
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;
        var dtos = list.Select(c => new AbandonedCartDto(
            Id: c.Id.ToString(),
            CartId: c.CartId.ToString(),
            CustomerName: c.CustomerName,
            CustomerPhone: c.CustomerPhone,
            TotalAmount: c.TotalAmount,
            ItemCount: c.ItemCount,
            Status: c.Status,
            CouponSent: c.CouponSent,
            IdleHours: Math.Round((now - c.LastActiveAt).TotalHours, 1),
            LastActive: c.LastActiveAt.ToString("yyyy/MM/dd - HH:mm")
        )).ToList();

        return Ok(ApiResponse<List<AbandonedCartDto>>.Ok(dtos, "لیست سبدهای رهاشده با موفقیت دریافت شد."));
    }

    /// <summary>
    /// ارسال پیامک ترغیبی و کوپن تخفیف برای بازیابی سبد خرید رهاشده
    /// </summary>
    [HttpPost("{id}/send-reminder")]
    [RequirePermission("marketing.update")]
    public async Task<IActionResult> SendReminder(
        string id,
        [FromBody] SendAbandonedCartReminderRequest request,
        CancellationToken ct)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            return BadRequest(new ApiErrorResponse(400, StandardErrorCodes.ValidationFailed, "شناسه نامعتبر است."));
        }

        var cart = await _db.AbandonedCartRecords.FirstOrDefaultAsync(c => c.Id == guid, ct);
        if (cart == null)
        {
            return NotFound(new ApiErrorResponse(404, StandardErrorCodes.NotFound, "سبد رهاشده مورد نظر یافت نشد."));
        }

        cart.MarkReminderSent(request.CouponCode);
        await _db.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "admin",
            User.FindFirstValue(ClaimTypes.Name) ?? "Admin",
            User.FindFirstValue(ClaimTypes.Role) ?? "super_admin",
            "ارسال پیامک بازیابی سبد خرید",
            $"سبد خرید {cart.CustomerName} ({cart.CustomerPhone})",
            "pending",
            $"ارسال کوپن {request.CouponCode} با {request.DiscountPercent}٪ تخفیف",
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            ct
        );

        return Ok(ApiResponse<object>.Ok(new
        {
            cartId = cart.CartId,
            phone = cart.CustomerPhone,
            couponCode = request.CouponCode,
            status = "reminder_sent",
            sentAt = DateTime.Now.ToString("yyyy/MM/dd - HH:mm")
        }, "پیامک وب‌سرویس با موفقیت به مشتری ارسال گردید و وضعیت به reminder_sent تغییر یافت."));
    }
}
