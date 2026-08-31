using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.DTOs;
using Shop.Application.Features.Orders.Commands;
using Shop.Application.Features.Orders.Queries;
using Shop.Application.Features.Payments.Commands;
using Shop.Application.Features.License.Queries;
using Shop.Domain.Entities;

namespace Shop.Api.Controllers;

/// <summary>
/// مدل درخواست نهایی‌سازی خرید و ثبت سفارش از سبد خرید
/// </summary>
/// <param name="ShippingAddress">آدرس کامل تحویل گیرنده</param>
/// <param name="DiscountAmount">مبلغ تخفیف اعمال‌شده (اختیاری)</param>
public record CheckoutRequest(AddressDto ShippingAddress, decimal DiscountAmount = 0);

/// <summary>
/// مدل درخواست تغییر وضعیت سفارش
/// </summary>
/// <param name="Status">وضعیت جدید سفارش (در انتظار، پرداخت شده، ارسال شده و ...)</param>
public record UpdateOrderStatusDto(OrderStatus Status);

/// <summary>
/// سرویس‌های ثبت، پیگیری و مدیریت سفارشات مشتریان و مدیران
/// </summary>
[ApiController]
[Route("api/orders")]
[Authorize]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    public OrdersController(ISender sender)
    {
        _sender = sender;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// نهایی‌سازی خرید و تبدیل سبد خرید فعلی به سفارش ثبت‌شده (با بررسی سهمیه لایسنس)
    /// </summary>
    /// <param name="request">آدرس ارسال سفارش و مبلغ تخفیف</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>مشخصات سفارش ثبت‌شده جهت پرداخت</returns>
    /// <response code="200">سفارش با موفقیت ثبت شد.</response>
    /// <response code="400">سبد خرید خالی است یا موجودی انبار کافی نیست.</response>
    /// <response code="403">سقف سفارشات مجاز لایسنس فروشگاه به پایان رسیده است.</response>
    [HttpPost("checkout")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<OrderDto>> Checkout([FromBody] CheckoutRequest request, CancellationToken ct)
    {
        var command = new CheckoutOrderCommand(GetUserId(), request.ShippingAddress, request.DiscountAmount);
        var result = await _sender.Send(command, ct);

        if (result.IsFailure)
        {
            if (result.Error.Code.StartsWith("License."))
            {
                return StatusCode(403, new { code = result.Error.Code, message = result.Error.Description });
            }

            return BadRequest(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// دریافت تاریخچه سفارشات ثبت‌شده توسط مشتری لاگین شده جاری با صفحه‌بندی
    /// </summary>
    /// <param name="page">شماره صفحه (پیش‌فرض ۱)</param>
    /// <param name="pageSize">تعداد آیتم در هر صفحه (پیش‌فرض ۱۰)</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>لیست صفحه‌بندی شده سفارشات مشتری</returns>
    /// <response code="200">لیست سفارشات مشتری بازگردانده شد.</response>
    [HttpGet("my-orders")]
    [ProducesResponseType(typeof(PaginatedResult<OrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<OrderDto>>> GetMyOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetCustomerOrdersQuery(GetUserId(), page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// دریافت اطلاعات کامل یک سفارش بر اساس شناسه یکتا (GUID)
    /// </summary>
    /// <param name="id">شناسه یکتای سفارش</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>جزئیات سفارش، اقلام و وضعیت پرداخت</returns>
    /// <response code="200">سفارش یافت شد و اطلاعات آن بازگردانده شد.</response>
    /// <response code="404">سفارش مورد نظر یافت نشد.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken ct)
    {
        var isAdmin = User.IsInRole("Admin");
        var result = await _sender.Send(new GetOrderByIdQuery(id, isAdmin ? null : GetUserId()), ct);

        if (result.IsFailure)
        {
            return NotFound(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// دریافت لیست سفارشات کل فروشگاه با امکان فیلتر بر اساس وضعیت (مخصوص مدیر سیستم)
    /// </summary>
    /// <param name="status">فیلتر اختیاری وضعیت سفارش (Pending, Paid, Shipped, Cancelled)</param>
    /// <param name="page">شماره صفحه</param>
    /// <param name="pageSize">تعداد آیتم در هر صفحه</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>لیست صفحه‌بندی شده سفارشات فروشگاه</returns>
    /// <response code="200">لیست سفارشات بازگردانده شد.</response>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PaginatedResult<OrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<OrderDto>>> GetAllAdmin([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetAdminOrdersQuery(status, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// تغییر وضعیت سفارش مانند ارسال، تحویل یا لغو (مخصوص مدیر سیستم)
    /// </summary>
    /// <param name="id">شناسه یکتای سفارش</param>
    /// <param name="dto">وضعیت جدید سفارش</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>پیام تأیید تغییر وضعیت</returns>
    /// <response code="200">وضعیت سفارش با موفقیت به‌روزرسانی شد.</response>
    /// <response code="400">تغییر وضعیت نامعتبر است یا سفارش یافت نشد.</response>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MessageResponseDto>> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusDto dto, CancellationToken ct)
    {
        var result = await _sender.Send(new UpdateOrderStatusCommand(id, dto.Status), ct);
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(new MessageResponseDto($"وضعیت سفارش با موفقیت به {dto.Status} تغییر یافت."));
    }
}

/// <summary>
/// مدل درخواست شبیه‌سازی پرداخت موفق
/// </summary>
/// <param name="TransactionReference">شماره پیگیری تراکنش بانکی (اختیاری)</param>
public record MockSuccessRequest(string? TransactionReference);

/// <summary>
/// سرویس‌های درگاه و تراکنش‌های پرداخت بانکی
/// </summary>
[ApiController]
[Route("api/payments")]
[Produces("application/json")]
public class PaymentsController : ControllerBase
{
    private readonly ISender _sender;

    public PaymentsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// ایجاد تراکنش و دریافت لینک اتصال به درگاه بانکی برای سفارش
    /// </summary>
    /// <param name="orderId">شناسه یکتای سفارش</param>
    /// <param name="gateway">نام درگاه پرداخت مورد نظر (پیش‌فرض MockGateway)</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>اطلاعات تراکنش ایجاد شده و آدرس اتصال به درگاه</returns>
    /// <response code="200">تراکنش ایجاد شد و آدرس درگاه پرداخت آماده است.</response>
    /// <response code="400">سفارش نامعتبر است یا قبلاً پرداخت شده است.</response>
    [HttpPost("initiate/{orderId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(PaymentInitiateResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaymentInitiateResponseDto>> Initiate(Guid orderId, [FromQuery] string gateway = "MockGateway", CancellationToken ct = default)
    {
        var result = await _sender.Send(new InitiatePaymentCommand(orderId, gateway), ct);
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// شبیه‌سازی کال‌بک پرداخت موفق بانکی و ثبت نهایی مصرف سهمیه لایسنس
    /// </summary>
    /// <param name="orderId">شناسه یکتای سفارش پرداخت‌شده</param>
    /// <param name="request">شماره ارجاع تراکنش</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>پیام تأیید پرداخت و به‌روزرسانی سفارش</returns>
    /// <response code="200">پرداخت با موفقیت تأیید شد و وضعیت سفارش به پرداخت‌شده تغییر کرد.</response>
    /// <response code="400">خطا در پردازش پرداخت.</response>
    [HttpPost("mock-success/{orderId:guid}")]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MessageResponseDto>> MockSuccess(Guid orderId, [FromBody] MockSuccessRequest? request, CancellationToken ct)
    {
        var result = await _sender.Send(new SimulatePaymentSuccessCommand(orderId, request?.TransactionReference), ct);
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(new MessageResponseDto("پرداخت با موفقیت پردازش شد، سفارش به وضعیت پرداخت‌شده تغییر کرد و مصرف لایسنس ثبت شد."));
    }
}

/// <summary>
/// سرویس‌های نظارت بر وضعیت لایسنس فعال روی فروشگاه و همگام‌سازی دستی
/// </summary>
[ApiController]
[Route("api/shop-license")]
[Produces("application/json")]
public class ShopLicenseController : ControllerBase
{
    private readonly ISender _sender;

    public ShopLicenseController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// دریافت وضعیت فعلی لایسنس فروشگاه، سقف سفارشات و تعداد مصرف‌شده
    /// </summary>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>اطلاعات وضعیت و محدودیت‌های لایسنس فعال</returns>
    /// <response code="200">وضعیت لایسنس فروشگاه با موفقیت ارسال شد.</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(ShopLicenseStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ShopLicenseStatusDto>> GetStatus(CancellationToken ct)
    {
        var result = await _sender.Send(new GetShopLicenseStatusQuery(), ct);
        return Ok(result);
    }

    /// <summary>
    /// همگام‌سازی فوری و استعلام مجدد وضعیت لایسنس از سرور مرکزی لایسنس (مخصوص مدیر سیستم)
    /// </summary>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>اطلاعات به‌روزشده لایسنس پس از همگام‌سازی</returns>
    /// <response code="200">همگام‌سازی لایسنس با موفقیت انجام شد.</response>
    [HttpPost("sync")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ShopLicenseStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ShopLicenseStatusDto>> Sync(CancellationToken ct)
    {
        var result = await _sender.Send(new SyncShopLicenseCommand(), ct);
        return Ok(result);
    }
}
