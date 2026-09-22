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
[Route("api/v1/admin/settings")]
[Authorize]
[Produces("application/json")]
public class AdminSettingsController : ControllerBase
{
    private readonly ShopDbContext _db;
    private readonly IAuditLogService _auditLogService;

    public AdminSettingsController(ShopDbContext db, IAuditLogService auditLogService)
    {
        _db = db;
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// دریافت تنظیمات کلی فروشگاه، درگاه‌های شاپرک و وب‌سرویس پیامک
    /// </summary>
    [HttpGet]
    [RequirePermission("settings.read")]
    public async Task<IActionResult> GetSettings(CancellationToken ct)
    {
        var s = await _db.StoreSettings.FirstOrDefaultAsync(ct) ?? new StoreSetting();

        var dto = new StoreSettingsDto(
            General: new StoreGeneralSettingsDto(
                s.StoreName,
                s.Phone,
                s.Email,
                s.Address
            ),
            Commerce: new StoreCommerceSettingsDto(
                s.Currency,
                s.FreeShippingThreshold,
                s.DefaultShippingFee,
                s.TaxPercent
            ),
            Payments: new StorePaymentsSettingsDto(
                Zarinpal: new GatewaySettingsDto(s.ZarinpalActive, s.ZarinpalMerchantId, null),
                Saman: new GatewaySettingsDto(s.SamanActive, null, s.SamanTerminalId)
            ),
            Sms: new StoreSmsSettingsDto(
                s.SmsProvider,
                s.SmsApiKey
            )
        );

        return Ok(ApiResponse<StoreSettingsDto>.Ok(dto));
    }

    /// <summary>
    /// ذخیره و به‌روزرسانی تنظیمات کلی فروشگاه چرم اورا
    /// </summary>
    [HttpPut]
    [RequirePermission("settings.manage")]
    public async Task<IActionResult> UpdateSettings(
        [FromBody] StoreSettingsDto dto,
        CancellationToken ct)
    {
        var s = await _db.StoreSettings.FirstOrDefaultAsync(ct);
        if (s == null)
        {
            s = new StoreSetting();
            _db.StoreSettings.Add(s);
        }

        if (dto.General != null)
        {
            s.StoreName = dto.General.StoreName;
            s.Phone = dto.General.Phone;
            s.Email = dto.General.Email;
            s.Address = dto.General.Address;
        }

        if (dto.Commerce != null)
        {
            s.Currency = dto.Commerce.Currency;
            s.FreeShippingThreshold = dto.Commerce.FreeShippingThreshold;
            s.DefaultShippingFee = dto.Commerce.DefaultShippingFee;
            s.TaxPercent = dto.Commerce.TaxPercent;
        }

        if (dto.Payments != null)
        {
            if (dto.Payments.Zarinpal != null)
            {
                s.ZarinpalActive = dto.Payments.Zarinpal.Active;
                if (!string.IsNullOrWhiteSpace(dto.Payments.Zarinpal.MerchantId))
                    s.ZarinpalMerchantId = dto.Payments.Zarinpal.MerchantId;
            }

            if (dto.Payments.Saman != null)
            {
                s.SamanActive = dto.Payments.Saman.Active;
                if (!string.IsNullOrWhiteSpace(dto.Payments.Saman.TerminalId))
                    s.SamanTerminalId = dto.Payments.Saman.TerminalId;
            }
        }

        if (dto.Sms != null)
        {
            s.SmsProvider = dto.Sms.Provider;
            if (!string.IsNullOrWhiteSpace(dto.Sms.ApiKey))
                s.SmsApiKey = dto.Sms.ApiKey;
        }

        await _db.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "admin",
            User.FindFirstValue(ClaimTypes.Name) ?? "Admin",
            User.FindFirstValue(ClaimTypes.Role) ?? "super_admin",
            "به‌روزرسانی تنظیمات فروشگاه",
            "تنظیمات سراسری سیستم",
            "نسخه قبلی",
            "به‌روزرسانی جدید",
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            ct
        );

        return Ok(ApiResponse<StoreSettingsDto>.Ok(dto, "تنظیمات فروشگاه با موفقیت ذخیره گردید."));
    }
}
