using LicenseServer.Domain.Enums;
using LicenseServer.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicenseServer.Api.Controllers;

[ApiController]
[Route("api/admin/licenses")]
[Authorize(Roles = "Admin")]
public class AdminLicensesController : ControllerBase
{
    private readonly ILicenseManager _licenseManager;

    public AdminLicensesController(ILicenseManager licenseManager)
    {
        _licenseManager = licenseManager;
    }

    [HttpPost]
    public async Task<IActionResult> IssueLicense([FromBody] IssueLicenseRequest request, CancellationToken ct)
    {
        var license = await _licenseManager.IssueLicenseAsync(request, ct);
        return CreatedAtAction(nameof(GetByKey), new { key = license.LicenseKey }, license);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] LicenseStatus? status, CancellationToken ct)
    {
        var list = await _licenseManager.GetAllLicensesAsync(status, ct);
        return Ok(list);
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> GetByKey(string key, CancellationToken ct)
    {
        var license = await _licenseManager.GetLicenseByKeyAsync(key, ct);
        if (license == null) return NotFound(new { message = "License not found." });
        return Ok(license);
    }

    public record UpgradeLicenseDto(LicenseType NewType, int? MaxOrders, DateTimeOffset? ExpirationDate);

    [HttpPut("{key}/upgrade")]
    public async Task<IActionResult> UpgradeLicense(string key, [FromBody] UpgradeLicenseDto dto, CancellationToken ct)
    {
        var updated = await _licenseManager.UpgradeLicenseAsync(key, dto.NewType, dto.MaxOrders, dto.ExpirationDate, ct);
        if (updated == null) return NotFound(new { message = "License not found." });
        return Ok(updated);
    }

    public record UpdateStatusDto(LicenseStatus Status);

    [HttpPatch("{key}/status")]
    public async Task<IActionResult> UpdateStatus(string key, [FromBody] UpdateStatusDto dto, CancellationToken ct)
    {
        var success = await _licenseManager.UpdateStatusAsync(key, dto.Status, ct);
        if (!success) return NotFound(new { message = "License not found." });
        return Ok(new { message = $"License status updated to {dto.Status} successfully." });
    }

    [HttpGet("{key}/logs")]
    public async Task<IActionResult> GetUsageLogs(string key, CancellationToken ct)
    {
        var logs = await _licenseManager.GetUsageLogsAsync(key, ct);
        return Ok(logs);
    }
}
