using LicenseServer.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace LicenseServer.Api.Controllers;

/// <summary>
/// مدل درخواست اعتبارسنجی لایسنس فروشگاه
/// </summary>
/// <param name="LicenseKey">کلید لایسنس برای بررسی اعتبار</param>
public record VerifyLicenseApiRequest(string LicenseKey);

/// <summary>
/// مدل درخواست ثبت مصرف سهمیه سفارش برای لایسنس
/// </summary>
/// <param name="LicenseKey">کلید لایسنس جهت کسر سهمیه سفارش</param>
public record RecordOrderApiRequest(string LicenseKey);

/// <summary>
/// سرویس‌های اعتبارسنجی خودکار و هارت‌بیت لایسنس برای ارتباط فروشگاه با سرور لایسنس
/// </summary>
[ApiController]
[Route("api/licensing")]
[Produces("application/json")]
public class LicensingController : ControllerBase
{
    private readonly ILicenseManager _licenseManager;

    public LicensingController(ILicenseManager licenseManager)
    {
        _licenseManager = licenseManager;
    }

    /// <summary>
    /// اعتبارسنجی رمزنگاری‌شده و بررسی سهمیه سفارشات لایسنس فروشگاه
    /// </summary>
    /// <param name="request">درخواست شامل کلید لایسنس</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>نتیجه اعتبارسنجی شامل امضای دیجیتال و وضعیت دسترسی‌ها</returns>
    /// <response code="200">عملیات بررسی انجام شد (فیلد IsValid وضعیت اعتبار را نشان می‌دهد).</response>
    /// <response code="404">کلید لایسنس در پایگاه داده یافت نشد.</response>
    [HttpPost("verify")]
    [ProducesResponseType(typeof(VerifyLicenseResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(VerifyLicenseResult), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VerifyLicenseResult>> Verify([FromBody] VerifyLicenseApiRequest request, CancellationToken ct)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _licenseManager.VerifyLicenseAsync(request.LicenseKey, clientIp, ct);

        if (!result.IsValid && result.Status == "NotFound")
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// ثبت یک سفارش جدید در سهمیه مصرفی لایسنس و بررسی عبور نکردن از سقف مجاز
    /// </summary>
    /// <param name="request">درخواست ثبت سفارش با کلید لایسنس</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>نتیجه کسر سهمیه و تعداد باقیمانده سفارشات مجاز</returns>
    /// <response code="200">سفارش با موفقیت در سهمیه لایسنس ثبت شد.</response>
    /// <response code="403">سقف سفارشات مجاز لایسنس پر شده یا لایسنس منقضی/معلق است.</response>
    [HttpPost("record-order")]
    [ProducesResponseType(typeof(RecordOrderUsageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RecordOrderUsageResult), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RecordOrderUsageResult>> RecordOrder([FromBody] RecordOrderApiRequest request, CancellationToken ct)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _licenseManager.RecordOrderUsageAsync(request.LicenseKey, clientIp, ct);

        if (!result.Success)
        {
            return StatusCode(403, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// نقطه تماس هارت‌بیت دوره‌ای برای همگام‌سازی پس‌زمینه لایسنس فروشگاه
    /// </summary>
    /// <param name="licenseKey">کلید لایسنس مورد نظر</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>وضعیت و مشخصات فعلی اعتبار لایسنس</returns>
    /// <response code="200">پاسخ هارت‌بیت و وضعیت لایسنس با موفقیت ارسال شد.</response>
    [HttpGet("heartbeat/{licenseKey}")]
    [ProducesResponseType(typeof(VerifyLicenseResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<VerifyLicenseResult>> Heartbeat(string licenseKey, CancellationToken ct)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _licenseManager.VerifyLicenseAsync(licenseKey, clientIp, ct);
        return Ok(result);
    }
}
