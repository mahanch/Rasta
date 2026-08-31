using LicenseServer.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace LicenseServer.Api.Controllers;

[ApiController]
[Route("api/admin/auth")]
public class AdminAuthController : ControllerBase
{
    private readonly ILicenseAuthService _authService;

    public AdminAuthController(ILicenseAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LicenseAdminLoginRequest request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request, ct);
        if (result == null)
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        return Ok(result);
    }
}
