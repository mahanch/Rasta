using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Filters;
using Shop.Application.Common.Models;
using Shop.Application.DTOs;
using Shop.Infrastructure.Persistence;

namespace Shop.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/audit-logs")]
[Authorize]
[Produces("application/json")]
public class AdminAuditLogsController : ControllerBase
{
    private readonly ShopDbContext _db;

    public AdminAuditLogsController(ShopDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// دریافت لاگ فعالیت‌های پرسنل با ذخیره مقادیر Before و After
    /// </summary>
    [HttpGet]
    [RequirePermission("audit.read")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 50,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        limit = Math.Max(1, limit);

        var query = _db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(a => a.AdminName.ToLower().Contains(s) ||
                                     a.Action.ToLower().Contains(s) ||
                                     a.Entity.ToLower().Contains(s));
        }

        var total = await query.CountAsync(ct);
        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(ct);

        var dtos = logs.Select(l => new AuditLogDto(
            Id: l.Id.ToString(),
            AdminName: l.AdminName,
            AdminRole: l.AdminRole,
            Action: l.Action,
            Entity: l.Entity,
            BeforeValue: l.BeforeValue,
            AfterValue: l.AfterValue,
            Ip: l.IpAddress,
            Timestamp: l.Timestamp
        )).ToList();

        return Ok(ApiResponse<object>.Ok(new { logs = dtos }, "لاگ‌های حسابرسی با موفقیت دریافت شدند.", new PaginationMeta(page, limit, total)));
    }
}
