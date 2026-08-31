using LicenseServer.Domain.Entities;
using LicenseServer.Domain.Enums;
using LicenseServer.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicenseServer.Api.Controllers;

/// <summary>
/// نتیجه و پیام عملیات‌های مدیریتی لایسنس
/// </summary>
/// <param name="Message">متن پیام وضعیت یا عملیات انجام شده</param>
public record AdminLicenseMessageResponse(string Message);

/// <summary>
/// مدل درخواست ارتقا یا تغییر مشخصات پلن لایسنس
/// </summary>
/// <param name="NewType">نوع جدید لایسنس (آزمایشی، استاندارد، نامحدود و ...)</param>
/// <param name="MaxOrders">حداکثر تعداد مجاز ثبت سفارش (در صورت نامحدود بودن null)</param>
/// <param name="ExpirationDate">تاریخ جدید انقضای لایسنس (در صورت دائمی بودن null)</param>
public record UpgradeLicenseDto(LicenseType NewType, int? MaxOrders, DateTimeOffset? ExpirationDate);

/// <summary>
/// مدل درخواست تغییر وضعیت فعال/غیرفعال بودن لایسنس
/// </summary>
/// <param name="Status">وضعیت جدید لایسنس (فعال، معلق، منقضی شده، باطل شده)</param>
public record UpdateStatusDto(LicenseStatus Status);

/// <summary>
/// سرویس‌های جامع مدیریت، صدور و ارتقای لایسنس‌ها (مخصوص مدیر سیستم)
/// </summary>
[ApiController]
[Route("api/admin/licenses")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class AdminLicensesController : ControllerBase
{
    private readonly ILicenseManager _licenseManager;

    public AdminLicensesController(ILicenseManager licenseManager)
    {
        _licenseManager = licenseManager;
    }

    /// <summary>
    /// صدور یک لایسنس جدید با امضای دیجیتال برای فروشگاه/مشتری
    /// </summary>
    /// <param name="request">اطلاعات خریدار، نوع پلن و محدودیت‌های سفارش</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>مشخصات کامل لایسنس صادر شده همراه با کلید اختصاصی</returns>
    /// <response code="201">لایسنس با موفقیت ایجاد و صادر شد.</response>
    /// <response code="400">اطلاعات ارسالی نامعتبر است.</response>
    [HttpPost]
    [ProducesResponseType(typeof(LicenseResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LicenseResponseDto>> IssueLicense([FromBody] IssueLicenseRequest request, CancellationToken ct)
    {
        var license = await _licenseManager.IssueLicenseAsync(request, ct);
        return CreatedAtAction(nameof(GetByKey), new { key = license.LicenseKey }, license);
    }

    /// <summary>
    /// دریافت لیست کامل تمام لایسنس‌های صادر شده با امکان فیلتر بر اساس وضعیت
    /// </summary>
    /// <param name="status">فیلتر اختیاری وضعیت لایسنس (فعال، معلق و ...)</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>لیست لایسنس‌های ثبت شده در سیستم</returns>
    /// <response code="200">لیست لایسنس‌ها با موفقیت بازگردانده شد.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<LicenseResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<LicenseResponseDto>>> GetAll([FromQuery] LicenseStatus? status, CancellationToken ct)
    {
        var list = await _licenseManager.GetAllLicensesAsync(status, ct);
        return Ok(list);
    }

    /// <summary>
    /// دریافت جزئیات و وضعیت یک لایسنس بر اساس کلید اختصاصی آن
    /// </summary>
    /// <param name="key">کلید لایسنس مورد نظر</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>مشخصات کامل لایسنس</returns>
    /// <response code="200">اطلاعات لایسنس یافت شد.</response>
    /// <response code="404">لایسنسی با این کلید یافت نشد.</response>
    [HttpGet("{key}")]
    [ProducesResponseType(typeof(LicenseResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LicenseResponseDto>> GetByKey(string key, CancellationToken ct)
    {
        var license = await _licenseManager.GetLicenseByKeyAsync(key, ct);
        if (license == null) return NotFound(new { message = "لایسنس مورد نظر یافت نشد." });
        return Ok(license);
    }

    /// <summary>
    /// ارتقا یا ویرایش نوع پلن، سقف سفارشات و تاریخ انقضای لایسنس
    /// </summary>
    /// <param name="key">کلید لایسنس مورد نظر</param>
    /// <param name="dto">اطلاعات پلن جدید و سقف سفارش</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>اطلاعات به‌روزشده لایسنس</returns>
    /// <response code="200">لایسنس با موفقیت ارتقا یافت.</response>
    /// <response code="404">لایسنس مورد نظر یافت نشد.</response>
    [HttpPut("{key}/upgrade")]
    [ProducesResponseType(typeof(LicenseResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LicenseResponseDto>> UpgradeLicense(string key, [FromBody] UpgradeLicenseDto dto, CancellationToken ct)
    {
        var updated = await _licenseManager.UpgradeLicenseAsync(key, dto.NewType, dto.MaxOrders, dto.ExpirationDate, ct);
        if (updated == null) return NotFound(new { message = "لایسنس مورد نظر یافت نشد." });
        return Ok(updated);
    }

    /// <summary>
    /// تغییر وضعیت فعال/غیرفعال بودن لایسنس (مانند مسدودسازی یا فعال‌سازی مجدد)
    /// </summary>
    /// <param name="key">کلید لایسنس مورد نظر</param>
    /// <param name="dto">وضعیت جدید مورد نظر</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>پیام تأیید تغییر وضعیت</returns>
    /// <response code="200">وضعیت لایسنس با موفقیت تغییر کرد.</response>
    /// <response code="404">لایسنس مورد نظر یافت نشد.</response>
    [HttpPatch("{key}/status")]
    [ProducesResponseType(typeof(AdminLicenseMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminLicenseMessageResponse>> UpdateStatus(string key, [FromBody] UpdateStatusDto dto, CancellationToken ct)
    {
        var success = await _licenseManager.UpdateStatusAsync(key, dto.Status, ct);
        if (!success) return NotFound(new { message = "لایسنس مورد نظر یافت نشد." });
        return Ok(new AdminLicenseMessageResponse($"وضعیت لایسنس با موفقیت به {dto.Status} تغییر یافت."));
    }

    /// <summary>
    /// دریافت تاریخچه لاگ‌های اعتبارسنجی و مصرف سهمیه سفارشات لایسنس
    /// </summary>
    /// <param name="key">کلید لایسنس مورد نظر</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>لیست رکوردهای ثبت شده از درخواست‌های اعتبارسنجی</returns>
    /// <response code="200">لیست لاگ‌های مصرف بازگردانده شد.</response>
    [HttpGet("{key}/logs")]
    [ProducesResponseType(typeof(List<LicenseUsageLog>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<LicenseUsageLog>>> GetUsageLogs(string key, CancellationToken ct)
    {
        var logs = await _licenseManager.GetUsageLogsAsync(key, ct);
        return Ok(logs);
    }
}
