using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Shop.Application.Common.Models;

namespace Shop.Api.Filters;

public class AdminApiResponseFilter : IAsyncResultFilter, IAsyncExceptionFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var path = context.HttpContext.Request.Path.Value ?? "";
        if (!path.StartsWith("/api/v1/admin", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        if (context.Result is FileResult)
        {
            await next();
            return;
        }

        if (context.Result is ObjectResult objectResult)
        {
            var value = objectResult.Value;
            var statusCode = objectResult.StatusCode ?? 200;

            if (value is ApiErrorResponse || (value != null && value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition() == typeof(ApiResponse<>)))
            {
                await next();
                return;
            }

            if (statusCode >= 200 && statusCode < 300)
            {
                var wrapped = new ApiResponse<object>
                {
                    Success = true,
                    StatusCode = statusCode,
                    Message = "عملیات با موفقیت انجام شد.",
                    Data = value
                };
                context.Result = new ObjectResult(wrapped) { StatusCode = statusCode };
            }
            else if (statusCode == 400 || statusCode == 422)
            {
                if (value is ValidationProblemDetails valProblem)
                {
                    var errors = valProblem.Errors
                        .SelectMany(kvp => kvp.Value.Select(msg => new ValidationErrorDetail(kvp.Key.ToLowerInvariant(), msg)))
                        .ToList();

                    context.Result = new ObjectResult(new ApiErrorResponse(
                        422,
                        StandardErrorCodes.ValidationFailed,
                        "اطلاعات ارسالی معتبر نمی‌باشد.",
                        errors
                    ))
                    {
                        StatusCode = 422
                    };
                }
                else if (value is string msg)
                {
                    context.Result = new ObjectResult(new ApiErrorResponse(statusCode, "ERROR", msg)) { StatusCode = statusCode };
                }
            }
        }
        else if (context.Result is NotFoundResult)
        {
            context.Result = new ObjectResult(new ApiErrorResponse(
                404,
                StandardErrorCodes.NotFound,
                "مورد درخواستی یافت نشد."
            ))
            {
                StatusCode = 404
            };
        }

        await next();
    }

    public Task OnExceptionAsync(ExceptionContext context)
    {
        var path = context.HttpContext.Request.Path.Value ?? "";
        if (path.StartsWith("/api/v1/admin", StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new ObjectResult(new ApiErrorResponse(
                500,
                "INTERNAL_SERVER_ERROR",
                context.Exception.Message
            ))
            {
                StatusCode = 500
            };
            context.ExceptionHandled = true;
        }

        return Task.CompletedTask;
    }
}
