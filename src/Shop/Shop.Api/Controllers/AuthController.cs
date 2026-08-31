using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.DTOs;
using Shop.Application.Features.Auth.Commands;

namespace Shop.Api.Controllers;

/// <summary>
/// سرویس‌های احراز هویت، ثبت‌نام، تمدید توکن و پروفایل کاربری مشتریان
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// ثبت‌نام حساب کاربری جدید برای مشتری در فروشگاه
    /// </summary>
    /// <param name="command">اطلاعات ثبت‌نام شامل ایمیل، کلمه عبور و نام کامل</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>اطلاعات احراز هویت شامل توکن دسترسی و توکن تجدید</returns>
    /// <response code="200">ثبت‌نام با موفقیت انجام شد و توکن صادر گردید.</response>
    /// <response code="400">اطلاعات نامعتبر است یا ایمیل قبلاً ثبت شده است.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterUserCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// ورود کاربر به سیستم با ایمیل و کلمه عبور و دریافت توکن JWT
    /// </summary>
    /// <param name="command">اطلاعات ورود شامل ایمیل و رمز عبور</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>توکن احراز هویت و اطلاعات کاربر</returns>
    /// <response code="200">ورود با موفقیت انجام شد.</response>
    /// <response code="401">ایمیل یا رمز عبور اشتباه است.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginUserCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        if (result.IsFailure)
        {
            return Unauthorized(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// تمدید و صدور توکن دسترسی جدید با استفاده از توکن تجدید (Refresh Token)
    /// </summary>
    /// <param name="command">توکن تجدید معتبر</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>توکن دسترسی و توکن تجدید جدید</returns>
    /// <response code="200">توکن با موفقیت تمدید شد.</response>
    /// <response code="400">توکن تجدید نامعتبر یا منقضی شده است.</response>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] RefreshTokenCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// دریافت مشخصات و دسترسی‌های کاربر لاگین شده جاری
    /// </summary>
    /// <returns>اطلاعات کاربری شامل شناسه، ایمیل و نقش</returns>
    /// <response code="200">اطلاعات کاربر با موفقیت ارسال شد.</response>
    /// <response code="401">کاربر احراز هویت نشده است.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<CurrentUserResponseDto> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var name = User.FindFirstValue(ClaimTypes.Name);
        var role = User.FindFirstValue(ClaimTypes.Role);

        return Ok(new CurrentUserResponseDto(
            UserId: userId,
            Email: email,
            FullName: name,
            Role: role
        ));
    }
}
