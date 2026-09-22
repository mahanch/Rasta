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
public class AdminUsersController : ControllerBase
{
    private readonly ShopDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogService _auditLogService;

    public AdminUsersController(
        ShopDbContext db,
        IPasswordHasher passwordHasher,
        IAuditLogService auditLogService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// دریافت لیست پرسنل و کاربران ادمین سیستم با صفحه‌بندی و جستجو
    /// </summary>
    [HttpGet("users")]
    [RequirePermission("users.read")]
    public async Task<IActionResult> GetAdminUsers(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        limit = Math.Max(1, limit);

        var query = _db.Users.AsNoTracking().Where(u => u.Role != "Customer");

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(s) || u.Email.ToLower().Contains(s));
        }

        var total = await query.CountAsync(ct);
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(ct);

        var roles = await _db.AdminRoles.AsNoTracking().ToListAsync(ct);

        var userDtos = users.Select(u =>
        {
            var roleObj = roles.FirstOrDefault(r => r.Name == u.Role.ToLowerInvariant());
            var permissions = roleObj?.Permissions ?? (u.Role == "super_admin" ? ["*"] : []);
            var roleNameFa = roleObj?.NameFa ?? u.Role;
            return new AdminUserDto(
                u.Id.ToString(),
                u.FullName,
                u.Email,
                u.Role,
                roleNameFa,
                permissions
            );
        }).ToList();

        var meta = new PaginationMeta(page, limit, total);
        return Ok(ApiResponse<List<AdminUserDto>>.Ok(userDtos, "لیست کاربران ادمین با موفقیت دریافت شد.", meta));
    }

    /// <summary>
    /// ایجاد یا دعوت کاربر ادمین جدید
    /// </summary>
    [HttpPost("users")]
    [RequirePermission("users.create")]
    public async Task<IActionResult> CreateAdminUser([FromBody] CreateAdminUserRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new ApiErrorResponse(
                400,
                StandardErrorCodes.ValidationFailed,
                "نام و ایمیل کاربر الزامی است."
            ));
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == normalizedEmail, ct))
        {
            return Conflict(new ApiErrorResponse(
                409,
                StandardErrorCodes.Conflict,
                "کاربری با این ایمیل قبلاً در سیستم ثبت شده است."
            ));
        }

        var defaultPasswordHash = _passwordHasher.HashPassword("Admin@Aura2026!");
        var user = new User(
            normalizedEmail,
            request.Name.Trim(),
            defaultPasswordHash,
            request.Role.Trim().ToLowerInvariant(),
            null
        );

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var roleObj = await _db.AdminRoles.FirstOrDefaultAsync(r => r.Name == user.Role, ct);
        var permissions = roleObj?.Permissions ?? [];
        var roleNameFa = roleObj?.NameFa ?? user.Role;

        var dto = new AdminUserDto(
            user.Id.ToString(),
            user.FullName,
            user.Email,
            user.Role,
            roleNameFa,
            permissions
        );

        return StatusCode(201, ApiResponse<AdminUserDto>.Created(dto, "کاربر ادمین با موفقیت ایجاد شد."));
    }

    /// <summary>
    /// دریافت ماتریس نقش‌ها و دسترسی‌های دانه‌ای (RBAC)
    /// </summary>
    [HttpGet("roles")]
    [RequirePermission("users.read")]
    public async Task<IActionResult> GetRoles(CancellationToken ct)
    {
        var roles = await _db.AdminRoles.AsNoTracking().ToListAsync(ct);
        var dtos = roles.Select(r => new RoleDto(
            r.Id.ToString(),
            r.Name,
            r.NameFa,
            r.Description,
            r.Permissions
        )).ToList();

        return Ok(ApiResponse<List<RoleDto>>.Ok(dtos));
    }

    /// <summary>
    /// به‌روزرسانی دسترسی‌های یک نقش ادمین
    /// </summary>
    [HttpPut("roles/{roleId}")]
    [RequirePermission("users.update")]
    public async Task<IActionResult> UpdateRolePermissions(
        string roleId,
        [FromBody] UpdateRolePermissionsRequest request,
        CancellationToken ct)
    {
        var role = Guid.TryParse(roleId, out var rGuid)
            ? await _db.AdminRoles.FirstOrDefaultAsync(r => r.Id == rGuid, ct)
            : await _db.AdminRoles.FirstOrDefaultAsync(r => r.Name == roleId.ToLowerInvariant(), ct);

        if (role == null)
        {
            return NotFound(new ApiErrorResponse(404, StandardErrorCodes.NotFound, "نقش مورد نظر یافت نشد."));
        }

        var before = string.Join(",", role.Permissions);
        role.UpdatePermissions(request.Permissions);
        await _db.SaveChangesAsync(ct);

        var after = string.Join(",", role.Permissions);
        await _auditLogService.LogAsync(
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "admin",
            User.FindFirstValue(ClaimTypes.Name) ?? "Admin",
            User.FindFirstValue(ClaimTypes.Role) ?? "super_admin",
            "به‌روزرسانی دسترسی‌های نقش",
            role.NameFa,
            before,
            after,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            ct
        );

        var dto = new RoleDto(
            role.Id.ToString(),
            role.Name,
            role.NameFa,
            role.Description,
            role.Permissions
        );

        return Ok(ApiResponse<RoleDto>.Ok(dto, "مجوزهای نقش با موفقیت به‌روزرسانی شد."));
    }
}
