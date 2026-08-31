using LicenseServer.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace LicenseServer.Api.Controllers;

/// <summary>
/// سرویس‌های احراز هویت و مدیریت دسترسی مدیران سرور لایسنس
/// </summary>
[ApiController]
[Route("api/admin/auth")]
[Produces("application/json")]
public class AdminAuthController : ControllerBase
{
    private readonly ILicenseAuthService _authService;

    public AdminAuthController(ILicenseAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// ورود ادمین به پنل مدیریت لایسنس و دریافت توکن JWT
    /// </summary>
    /// <param name="request">اطلاعات کاربری شامل نام کاربری و کلمه عبور</param>
    /// <param name="ct">توکن لغو عملیات (CancellationToken)</param>
    /// <returns>توکن دسترسی JWT و اطلاعات مدیر</returns>
    /// <response code="200">احراز هویت با موفقیت انجام شد و توکن صادر گردید.</response>
    /// <response code="401">نام کاربری یا کلمه عبور نامعتبر است.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LicenseAdminLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LicenseAdminLoginResponse>> Login([FromBody] LicenseAdminLoginRequest request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request, ct);
        if (result == null)
        {
            return Unauthorized(new { message = "نام کاربری یا کلمه عبور اشتباه است." });
        }

        return Ok(result);
    }
}
