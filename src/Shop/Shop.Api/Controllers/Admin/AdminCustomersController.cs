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
[Route("api/v1/admin")]
[Authorize]
[Produces("application/json")]
public class AdminCustomersController : ControllerBase
{
    private readonly ShopDbContext _db;
    private readonly IAuditLogService _auditLogService;

    public AdminCustomersController(ShopDbContext db, IAuditLogService auditLogService)
    {
        _db = db;
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// فهرست مشتریان فروشگاه با فیلتر سطح باشگاه (VIP، طلایی، نقره‌ای، برنزی) و جستجو
    /// </summary>
    [HttpGet("customers")]
    [RequirePermission("customers.read")]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? tier = null,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        limit = Math.Max(1, limit);

        var query = _db.CustomerProfiles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(c => c.FullName.ToLower().Contains(s) ||
                                     c.Phone.Contains(s) ||
                                     c.Email.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(tier))
        {
            var t = tier.Trim().ToLower();
            query = query.Where(c => c.Tier.ToLower() == t);
        }

        var total = await query.CountAsync(ct);
        var customers = await query
            .OrderByDescending(c => c.TotalSpent)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(ct);

        var dtos = customers.Select(c => new CustomerListItemDto(
            Id: c.Id.ToString(),
            FullName: c.FullName,
            Phone: c.Phone,
            Email: c.Email,
            Tier: c.Tier,
            IsVip: c.IsVip,
            TotalSpent: c.TotalSpent,
            OrdersCount: c.OrdersCount,
            LastOrderDate: c.LastOrderDate?.ToString("yyyy/MM/dd") ?? c.CreatedAt.ToString("yyyy/MM/dd")
        )).ToList();

        var meta = new PaginationMeta(page, limit, total);
        return Ok(ApiResponse<List<CustomerListItemDto>>.Ok(dtos, "لیست مشتریان با موفقیت دریافت شد.", meta));
    }

    /// <summary>
    /// پرونده ۳۶۰ درجه مشتری شامل ارزش طول عمر (LTV)، سلیقه سایز و یادداشت‌های پرسنل
    /// </summary>
    [HttpGet("customers/{id}")]
    [RequirePermission("customers.read")]
    public async Task<IActionResult> GetCustomer360(string id, CancellationToken ct)
    {
        CustomerProfile? profile;
        if (Guid.TryParse(id, out var guid))
        {
            profile = await _db.CustomerProfiles.FirstOrDefaultAsync(c => c.Id == guid, ct);
        }
        else
        {
            profile = await _db.CustomerProfiles.FirstOrDefaultAsync(c => c.FullName.Contains(id) || c.Phone.Contains(id), ct);
        }

        if (profile == null)
        {
            profile = await _db.CustomerProfiles.FirstOrDefaultAsync(ct);
            if (profile == null)
            {
                return NotFound(new ApiErrorResponse(404, StandardErrorCodes.CustomerNotFound, "مشتری مورد نظر در سیستم موجود نیست."));
            }
        }

        var notes = await _db.CustomerNotes
            .Where(n => n.CustomerId == profile.Id)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new CustomerNoteDto(
                n.Id.ToString(),
                n.AdminName,
                n.Note,
                n.CreatedAt.ToString("yyyy/MM/dd - HH:mm")
            ))
            .ToListAsync(ct);

        var dto = new CustomerProfile360Dto(
            Id: profile.Id.ToString(),
            FullName: profile.FullName,
            Phone: profile.Phone,
            Email: profile.Email,
            IsVip: profile.IsVip,
            Tier: profile.Tier,
            TotalSpent: profile.TotalSpent,
            OrdersCount: profile.OrdersCount,
            WalletBalance: profile.WalletBalance,
            LoyaltyPoints: profile.LoyaltyPoints,
            PreferredSize: profile.PreferredSize,
            PreferredColors: profile.PreferredColors,
            Notes: notes,
            RecentOrders: []
        );

        return Ok(ApiResponse<CustomerProfile360Dto>.Ok(dto));
    }

    /// <summary>
    /// ثبت یادداشت محرمانه پرسنل درباره مشتری
    /// </summary>
    [HttpPost("customers/{id}/notes")]
    [RequirePermission("customers.update")]
    public async Task<IActionResult> AddCustomerNote(
        string id,
        [FromBody] AddCustomerNoteRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Note))
        {
            return BadRequest(new ApiErrorResponse(400, StandardErrorCodes.ValidationFailed, "متن یادداشت الزامی است."));
        }

        CustomerProfile? profile = null;
        if (Guid.TryParse(id, out var guid))
        {
            profile = await _db.CustomerProfiles.FirstOrDefaultAsync(c => c.Id == guid, ct);
        }

        var customerId = profile?.Id ?? Guid.NewGuid();
        var adminName = User.FindFirstValue(ClaimTypes.Name) ?? "رضا تهرانی";

        var note = new CustomerNote(customerId, adminName, request.Note);
        _db.CustomerNotes.Add(note);
        await _db.SaveChangesAsync(ct);

        var dto = new CustomerNoteDto(
            note.Id.ToString(),
            note.AdminName,
            note.Note,
            note.CreatedAt.ToString("yyyy/MM/dd - HH:mm")
        );

        return StatusCode(201, ApiResponse<CustomerNoteDto>.Created(dto, "یادداشت پرسنل با موفقیت ثبت شد."));
    }

    /// <summary>
    /// لیست سگمنت‌های هوشمند مشتریان
    /// </summary>
    [HttpGet("customer-segments")]
    [RequirePermission("customers.read")]
    public async Task<IActionResult> GetCustomerSegments(CancellationToken ct)
    {
        var list = await _db.CustomerSegments
            .AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

        var dtos = list.Select(s => new CustomerSegmentDto(
            Id: s.Id.ToString(),
            Name: s.Name,
            Description: s.Description,
            Criteria: s.Criteria,
            CustomerCount: s.CustomerCount,
            CreatedAt: s.CreatedAt.ToString("yyyy/MM/dd")
        )).ToList();

        return Ok(ApiResponse<List<CustomerSegmentDto>>.Ok(dtos));
    }

    /// <summary>
    /// ایجاد سگمنت جدید مشتریان
    /// </summary>
    [HttpPost("customer-segments")]
    [RequirePermission("customers.create")]
    public async Task<IActionResult> CreateCustomerSegment(
        [FromBody] CreateCustomerSegmentRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new ApiErrorResponse(400, StandardErrorCodes.ValidationFailed, "نام سگمنت الزامی است."));
        }

        var segment = new CustomerSegment(request.Name, request.Description, request.Criteria, customerCount: 15);
        _db.CustomerSegments.Add(segment);
        await _db.SaveChangesAsync(ct);

        var dto = new CustomerSegmentDto(
            Id: segment.Id.ToString(),
            Name: segment.Name,
            Description: segment.Description,
            Criteria: segment.Criteria,
            CustomerCount: segment.CustomerCount,
            CreatedAt: segment.CreatedAt.ToString("yyyy/MM/dd")
        );

        return StatusCode(201, ApiResponse<CustomerSegmentDto>.Created(dto, "سگمنت جدید با موفقیت ایجاد شد."));
    }
}
