using LicenseServer.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace LicenseServer.Api.Controllers;

public record VerifyLicenseApiRequest(string LicenseKey);
public record RecordOrderApiRequest(string LicenseKey);

[ApiController]
[Route("api/licensing")]
public class LicensingController : ControllerBase
{
    private readonly ILicenseManager _licenseManager;

    public LicensingController(ILicenseManager licenseManager)
    {
        _licenseManager = licenseManager;
    }

    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] VerifyLicenseApiRequest request, CancellationToken ct)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _licenseManager.VerifyLicenseAsync(request.LicenseKey, clientIp, ct);

        if (!result.IsValid && result.Status == "NotFound")
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPost("record-order")]
    public async Task<IActionResult> RecordOrder([FromBody] RecordOrderApiRequest request, CancellationToken ct)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _licenseManager.RecordOrderUsageAsync(request.LicenseKey, clientIp, ct);

        if (!result.Success)
        {
            return StatusCode(403, result);
        }

        return Ok(result);
    }

    [HttpGet("heartbeat/{licenseKey}")]
    public async Task<IActionResult> Heartbeat(string licenseKey, CancellationToken ct)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _licenseManager.VerifyLicenseAsync(licenseKey, clientIp, ct);
        return Ok(result);
    }
}
