using System.Text.Json.Serialization;

namespace Shop.Application.Common.Models;

public class PaginationMeta
{
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    public PaginationMeta() { }

    public PaginationMeta(int page, int limit, int total)
    {
        Page = page;
        Limit = limit;
        Total = total;
        TotalPages = limit > 0 ? (int)Math.Ceiling((double)total / limit) : 1;
    }
}

public class ApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; } = true;

    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; } = 200;

    [JsonPropertyName("message")]
    public string Message { get; set; } = "عملیات با موفقیت انجام شد.";

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("meta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PaginationMeta? Meta { get; set; }

    public ApiResponse() { }

    public ApiResponse(T? data, string message = "عملیات با موفقیت انجام شد.", int statusCode = 200, PaginationMeta? meta = null)
    {
        Success = true;
        StatusCode = statusCode;
        Message = message;
        Data = data;
        Meta = meta;
    }

    public static ApiResponse<T> Ok(T? data, string message = "عملیات با موفقیت انجام شد.", PaginationMeta? meta = null)
        => new(data, message, 200, meta);

    public static ApiResponse<T> Created(T? data, string message = "مورد جدید با موفقیت ایجاد شد.")
        => new(data, message, 201);
}

public class ValidationErrorDetail
{
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    public ValidationErrorDetail() { }

    public ValidationErrorDetail(string field, string message)
    {
        Field = field;
        Message = message;
    }
}

public class ApiErrorResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; } = false;

    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; } = 400;

    [JsonPropertyName("errorCode")]
    public string ErrorCode { get; set; } = "ERROR";

    [JsonPropertyName("message")]
    public string Message { get; set; } = "خطایی رخ داده است.";

    [JsonPropertyName("errors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ValidationErrorDetail>? Errors { get; set; }

    public ApiErrorResponse() { }

    public ApiErrorResponse(int statusCode, string errorCode, string message, List<ValidationErrorDetail>? errors = null)
    {
        Success = false;
        StatusCode = statusCode;
        ErrorCode = errorCode;
        Message = message;
        Errors = errors ?? [];
    }
}
