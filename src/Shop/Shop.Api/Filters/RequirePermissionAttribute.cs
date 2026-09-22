using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Shop.Application.Common.Models;

namespace Shop.Api.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _permission;

    public RequirePermissionAttribute(string permission)
    {
        _permission = permission;
    }

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
        {
            context.Result = new ObjectResult(new ApiErrorResponse(
                401,
                StandardErrorCodes.Unauthorized,
                "توکن JWT منقضی شده یا ارائه نشده است."
            ))
            {
                StatusCode = 401
            };
            return Task.CompletedTask;
        }

        var role = user.FindFirstValue(ClaimTypes.Role);
        if (string.Equals(role, "super_admin", StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        var permissions = user.FindAll("permission").Select(c => c.Value).ToList();
        if (permissions.Contains("*") || permissions.Contains(_permission))
        {
            return Task.CompletedTask;
        }

        // Check for wildcards like "products.*"
        var dotIndex = _permission.IndexOf('.');
        if (dotIndex > 0)
        {
            var wildcard = _permission[..(dotIndex + 1)] + "*";
            if (permissions.Contains(wildcard))
            {
                return Task.CompletedTask;
            }
        }

        context.Result = new ObjectResult(new ApiErrorResponse(
            403,
            StandardErrorCodes.ForbiddenPermission,
            "نقش کاربر فاقد مجوز دانه‌ای مورد نیاز برای این عملیات است."
        ))
        {
            StatusCode = 403
        };

        return Task.CompletedTask;
    }
}
