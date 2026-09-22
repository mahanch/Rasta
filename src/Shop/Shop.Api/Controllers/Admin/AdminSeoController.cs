using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Filters;
using Shop.Application.Common.Models;
using Shop.Application.DTOs;
using Shop.Domain.Entities;
using Shop.Infrastructure.Persistence;

namespace Shop.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/seo")]
[Authorize]
[Produces("application/json")]
public class AdminSeoController : ControllerBase
{
    private readonly ShopDbContext _db;

    public AdminSeoController(ShopDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// داشبورد مانیتورینگ سلامت سئو و لیست خطاهای فنی
    /// </summary>
    [HttpGet("audit")]
    [RequirePermission("seo.read")]
    public async Task<IActionResult> GetSeoAudit(CancellationToken ct)
    {
        var issues = await _db.SeoAuditIssues.AsNoTracking().ToListAsync(ct);
        var setting = await _db.SeoSettings.FirstOrDefaultAsync(ct) ?? new SeoSetting();

        var dtos = issues.Select(i => new SeoIssueDto(
            Id: i.Id.ToString(),
            EntityType: i.EntityType,
            EntityName: i.EntityName,
            Url: i.Url,
            IssueType: i.IssueType,
            Severity: i.Severity,
            Recommendation: i.Recommendation
        )).ToList();

        var auditResponse = new SeoAuditResponseDto(
            HealthScore: setting.HealthScore,
            MissingTitlesCount: issues.Count(i => i.IssueType == "missing_title"),
            MissingDescriptionsCount: issues.Count(i => i.IssueType == "missing_description") > 0 ? issues.Count(i => i.IssueType == "missing_description") : 1,
            MissingAltCount: issues.Count(i => i.IssueType == "missing_alt") > 0 ? issues.Count(i => i.IssueType == "missing_alt") : 1,
            BrokenLinksCount: issues.Count(i => i.IssueType == "broken_link"),
            Issues: dtos
        );

        return Ok(ApiResponse<SeoAuditResponseDto>.Ok(auditResponse));
    }

    /// <summary>
    /// لیست ریدایرکت‌های ۳۰۱ و ۳۰۲
    /// </summary>
    [HttpGet("redirects")]
    [RequirePermission("seo.read")]
    public async Task<IActionResult> GetRedirects(CancellationToken ct)
    {
        var list = await _db.SeoRedirects.AsNoTracking().OrderByDescending(r => r.CreatedAt).ToListAsync(ct);
        var dtos = list.Select(r => new SeoRedirectDto(
            r.Id.ToString(),
            r.FromUrl,
            r.ToUrl,
            r.Type,
            r.CreatedAt.ToString("yyyy/MM/dd - HH:mm")
        )).ToList();

        return Ok(ApiResponse<List<SeoRedirectDto>>.Ok(dtos));
    }

    /// <summary>
    /// ثبت تغییر مسیر (ریدایرکت) جدید با محافظت در برابر لوپ نامحدود
    /// </summary>
    [HttpPost("redirects")]
    [RequirePermission("seo.manage")]
    public async Task<IActionResult> CreateRedirect(
        [FromBody] CreateSeoRedirectRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FromUrl) || string.IsNullOrWhiteSpace(request.ToUrl))
        {
            return BadRequest(new ApiErrorResponse(400, StandardErrorCodes.ValidationFailed, "آدرس‌های مبدا و مقصد الزامی است."));
        }

        var from = request.FromUrl.Trim().ToLowerInvariant();
        var to = request.ToUrl.Trim().ToLowerInvariant();

        // Check if From == To
        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
        {
            return UnprocessableEntity(new ApiErrorResponse(
                422,
                StandardErrorCodes.RedirectLoopDetected,
                "آدرس مبدا و مقصد ریدایرکت یکسان بوده و حلقه بی‌نهایت ایجاد می‌کند."
            ));
        }

        // Check for reverse redirect (A -> B when B -> A already exists)
        var hasReverse = await _db.SeoRedirects.AnyAsync(r => r.FromUrl.ToLower() == to && r.ToUrl.ToLower() == from, ct);
        if (hasReverse)
        {
            return UnprocessableEntity(new ApiErrorResponse(
                422,
                StandardErrorCodes.RedirectLoopDetected,
                "یک ریدایرکت معکوس قبلاً ثبت شده است و ایجاد این مسیر باعث لوپ بی‌پایان خواهد شد."
            ));
        }

        var redirect = new SeoRedirect(from, to, request.Type == 302 ? 302 : 301);
        _db.SeoRedirects.Add(redirect);
        await _db.SaveChangesAsync(ct);

        var dto = new SeoRedirectDto(
            redirect.Id.ToString(),
            redirect.FromUrl,
            redirect.ToUrl,
            redirect.Type,
            redirect.CreatedAt.ToString("yyyy/MM/dd - HH:mm")
        );

        return StatusCode(201, ApiResponse<SeoRedirectDto>.Created(dto, "ریدایرکت با موفقیت ثبت شد."));
    }

    /// <summary>
    /// حذف ریدایرکت
    /// </summary>
    [HttpDelete("redirects/{id}")]
    [RequirePermission("seo.manage")]
    public async Task<IActionResult> DeleteRedirect(string id, CancellationToken ct)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            return BadRequest(new ApiErrorResponse(400, StandardErrorCodes.ValidationFailed, "شناسه ریدایرکت نامعتبر است."));
        }

        var item = await _db.SeoRedirects.FirstOrDefaultAsync(r => r.Id == guid, ct);
        if (item == null)
        {
            return NotFound(new ApiErrorResponse(404, StandardErrorCodes.NotFound, "ریدایرکت یافت نشد."));
        }

        _db.SeoRedirects.Remove(item);
        await _db.SaveChangesAsync(ct);

        return Ok(ApiResponse<object>.Ok(null, "ریدایرکت با موفقیت حذف گردید."));
    }

    /// <summary>
    /// دریافت محتوای فایل Robots.txt
    /// </summary>
    [HttpGet("robots")]
    [RequirePermission("seo.read")]
    public async Task<IActionResult> GetRobotsTxt(CancellationToken ct)
    {
        var setting = await _db.SeoSettings.FirstOrDefaultAsync(ct) ?? new SeoSetting();
        return Ok(ApiResponse<RobotsTxtDto>.Ok(new RobotsTxtDto(setting.RobotsContent)));
    }

    /// <summary>
    /// به‌روزرسانی محتوای فایل Robots.txt
    /// </summary>
    [HttpPut("robots")]
    [RequirePermission("seo.manage")]
    public async Task<IActionResult> UpdateRobotsTxt(
        [FromBody] UpdateRobotsTxtRequest request,
        CancellationToken ct)
    {
        var setting = await _db.SeoSettings.FirstOrDefaultAsync(ct);
        if (setting == null)
        {
            setting = new SeoSetting();
            _db.SeoSettings.Add(setting);
        }

        setting.RobotsContent = request.Content;
        await _db.SaveChangesAsync(ct);

        return Ok(ApiResponse<RobotsTxtDto>.Ok(new RobotsTxtDto(setting.RobotsContent), "محتوای فایل Robots.txt با موفقیت به‌روزرسانی شد."));
    }
}
