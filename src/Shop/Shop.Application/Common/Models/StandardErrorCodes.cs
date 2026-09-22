namespace Shop.Application.Common.Models;

public static class StandardErrorCodes
{
    public const string Unauthorized = "UNAUTHORIZED";
    public const string ForbiddenPermission = "FORBIDDEN_PERMISSION";
    public const string SkuAlreadyExists = "SKU_ALREADY_EXISTS";
    public const string InsufficientStock = "INSUFFICIENT_STOCK";
    public const string RedirectLoopDetected = "REDIRECT_LOOP_DETECTED";
    public const string CouponExpired = "COUPON_EXPIRED";
    public const string CouponLimitReached = "COUPON_LIMIT_REACHED";
    public const string CustomerNotFound = "CUSTOMER_NOT_FOUND";
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string NotFound = "NOT_FOUND";
    public const string Conflict = "CONFLICT";
}
