using System.Text.Json;
using Shop.Application.Common.Interfaces;

namespace Shop.Api.Middleware;

public class LicenseValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LicenseValidationMiddleware> _logger;

    public LicenseValidationMiddleware(RequestDelegate next, ILogger<LicenseValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ILicenseClientService licenseService)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        // Only enforce license check on critical operational routes, avoiding health checks or license info itself
        if (path.StartsWith("/api/orders/checkout") || (context.Request.Method == "POST" && path.StartsWith("/api/products")))
        {
            var licenseState = licenseService.GetCurrentLicenseState();
            
            // If license is completely suspended or revoked
            if (licenseState.Status == "Suspended" || licenseState.Status == "Revoked")
            {
                _logger.LogWarning("Blocking request to {Path} due to suspended/revoked license state: {Status}", path, licenseState.Status);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    code = "License.Suspended",
                    message = $"Store operations are currently suspended. Status: {licenseState.Status}. Please contact administrator."
                }));
                return;
            }
        }

        await _next(context);
    }
}
