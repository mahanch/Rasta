using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Application.Common.Models;
using Shop.Application.DTOs;
using Shop.Domain.Entities;
using Shop.Infrastructure.Persistence;

namespace Shop.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/auth")]
[Produces("application/json")]
public class AdminAuthController : ControllerBase
{
    private readonly ShopDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AdminAuthController(
        ShopDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    /// <summary>
    /// ورود کاربر ادمین به پنل مدیریت با نام کاربری و کلمه عبور
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] AdminLoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ApiErrorResponse(
                400,
                StandardErrorCodes.ValidationFailed,
                "ایمیل و کلمه عبور الزامی است.",
                [new ValidationErrorDetail("email", "ایمیل الزامی است.")]
            ));
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Unauthorized(new ApiErrorResponse(
                401,
                StandardErrorCodes.Unauthorized,
                "ایمیل یا کلمه عبور اشتباه است."
            ));
        }

        var roleObj = await _db.AdminRoles.FirstOrDefaultAsync(r => r.Name == user.Role.ToLowerInvariant(), ct);
        var permissions = roleObj?.Permissions ?? (user.Role == "super_admin" || user.Role == "Admin" ? ["*"] : []);
        var roleNameFa = roleObj?.NameFa ?? (user.Role == "super_admin" ? "مدیر ارشد پلتفرم" : "کاربر سیستم");

        var response = _jwtTokenService.GenerateAdminTokens(
            user.Id,
            user.Email,
            user.FullName,
            user.Role,
            roleNameFa,
            permissions
        );

        user.AddRefreshToken(response.RefreshToken, DateTimeOffset.UtcNow.AddDays(7));
        user.RecordLogin();
        await _db.SaveChangesAsync(ct);

        return Ok(ApiResponse<AdminLoginResponse>.Ok(response, "ورود با موفقیت انجام شد."));
    }

    /// <summary>
    /// دریافت مشخصات و دسترسی‌های کاربر ادمین جاری لاگین شده
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
        {
            return Unauthorized(new ApiErrorResponse(401, StandardErrorCodes.Unauthorized, "کاربر احراز هویت نشده است."));
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);
        if (user == null)
        {
            return NotFound(new ApiErrorResponse(404, StandardErrorCodes.CustomerNotFound, "کاربر یافت نشد."));
        }

        var roleObj = await _db.AdminRoles.FirstOrDefaultAsync(r => r.Name == user.Role.ToLowerInvariant(), ct);
        var permissions = roleObj?.Permissions ?? (user.Role == "super_admin" || user.Role == "Admin" ? ["*"] : []);
        var roleNameFa = roleObj?.NameFa ?? (user.Role == "super_admin" ? "مدیر ارشد پلتفرم" : "کاربر سیستم");

        var userDto = new AdminUserDto(
            user.Id.ToString(),
            user.FullName,
            user.Email,
            user.Role,
            roleNameFa,
            permissions
        );

        return Ok(ApiResponse<AdminUserDto>.Ok(userDto));
    }
}
