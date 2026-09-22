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
[Route("api/v1/admin/returns")]
[Authorize]
[Produces("application/json")]
public class AdminReturnsController : ControllerBase
{
    private readonly ShopDbContext _db;
    private readonly IAuditLogService _auditLogService;

    public AdminReturnsController(ShopDbContext db, IAuditLogService auditLogService)
    {
        _db = db;
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// فهرست درخواست‌های عودت و تعویض سایز کفش با فیلتر وضعیت
    /// </summary>
    [HttpGet]
    [RequirePermission("returns.manage")]
    public async Task<IActionResult> GetReturns(
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var query = _db.ReturnRequests.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = status.Trim().ToLower();
            query = query.Where(r => r.Status.ToLower() == s);
        }

        var list = await query.OrderByDescending(r => r.CreatedAt).ToListAsync(ct);

        var dtos = list.Select(r => new ReturnRequestDto(
            Id: r.Id.ToString(),
            OrderId: r.OrderId.ToString(),
            OrderNumber: r.OrderNumber,
            CustomerName: r.CustomerName,
            CustomerPhone: r.CustomerPhone,
            Reason: r.Reason,
            RequestedSize: r.RequestedSize,
            SoleCondition: r.SoleCondition,
            LeatherCondition: r.LeatherCondition,
            Status: r.Status,
            AdminNotes: r.AdminNotes,
            RefundAmount: r.RefundAmount,
            CreatedAt: r.CreatedAt.ToString("yyyy/MM/dd - HH:mm")
        )).ToList();

        return Ok(ApiResponse<List<ReturnRequestDto>>.Ok(dtos, "لیست درخواست‌های مرجوعی دریافت شد."));
    }

    /// <summary>
    /// به‌روزرسانی وضعیت مرجوعی، ثبت نظر کارشناسی سلامت زیره/چرم و صدور دستور بازپرداخت
    /// </summary>
    [HttpPatch("{id}/status")]
    [RequirePermission("returns.manage")]
    public async Task<IActionResult> UpdateReturnStatus(
        string id,
        [FromBody] UpdateReturnStatusRequest request,
        CancellationToken ct)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            return BadRequest(new ApiErrorResponse(400, StandardErrorCodes.ValidationFailed, "شناسه مرجوعی نامعتبر است."));
        }

        var item = await _db.ReturnRequests.FirstOrDefaultAsync(r => r.Id == guid, ct);
        if (item == null)
        {
            return NotFound(new ApiErrorResponse(404, StandardErrorCodes.NotFound, "درخواست مرجوعی یافت نشد."));
        }

        var oldStatus = item.Status;
        item.UpdateStatus(request.Status, request.AdminNotes, request.RefundAmount);
        await _db.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "admin",
            User.FindFirstValue(ClaimTypes.Name) ?? "Admin",
            User.FindFirstValue(ClaimTypes.Role) ?? "super_admin",
            "به‌روزرسانی وضعیت مرجوعی",
            $"سفارش {item.OrderNumber} ({item.CustomerName})",
            oldStatus,
            $"{request.Status} - {request.AdminNotes}",
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            ct
        );

        var dto = new ReturnRequestDto(
            Id: item.Id.ToString(),
            OrderId: item.OrderId.ToString(),
            OrderNumber: item.OrderNumber,
            CustomerName: item.CustomerName,
            CustomerPhone: item.CustomerPhone,
            Reason: item.Reason,
            RequestedSize: item.RequestedSize,
            SoleCondition: item.SoleCondition,
            LeatherCondition: item.LeatherCondition,
            Status: item.Status,
            AdminNotes: item.AdminNotes,
            RefundAmount: item.RefundAmount,
            CreatedAt: item.CreatedAt.ToString("yyyy/MM/dd - HH:mm")
        );

        return Ok(ApiResponse<ReturnRequestDto>.Ok(dto, "وضعیت مرجوعی با موفقیت به‌روزرسانی شد."));
    }
}
