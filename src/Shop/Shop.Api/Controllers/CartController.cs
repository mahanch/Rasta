using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.DTOs;
using Shop.Application.Features.Cart.Commands;
using Shop.Application.Features.Cart.Queries;

namespace Shop.Api.Controllers;

/// <summary>
/// مدل درخواست افزودن کالا به سبد خرید
/// </summary>
/// <param name="ProductId">شناسه یکتای محصول</param>
/// <param name="Quantity">تعداد مورد نظر جهت افزودن به سبد (حداقل ۱)</param>
public record AddCartItemRequest(Guid ProductId, int Quantity);

/// <summary>
/// مدل درخواست تغییر تعداد کالا در سبد خرید
/// </summary>
/// <param name="Quantity">تعداد جدید کالا</param>
public record UpdateCartItemRequest(int Quantity);

/// <summary>
/// سرویس‌های مدیریت سبد خرید مشتری (نیازمند احراز هویت)
/// </summary>
[ApiController]
[Route("api/cart")]
[Authorize]
[Produces("application/json")]
public class CartController : ControllerBase
{
    private readonly ISender _sender;

    public CartController(ISender sender)
    {
        _sender = sender;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// دریافت سبد خرید کاربر جاری همراه با قیمت‌ها و تخفیفات محاسبه‌شده
    /// </summary>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>اطلاعات کامل سبد خرید و لیست اقلام</returns>
    /// <response code="200">سبد خرید با موفقیت بازگردانده شد.</response>
    /// <response code="401">کاربر احراز هویت نشده است.</response>
    [HttpGet]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CartDto>> GetCart(CancellationToken ct)
    {
        var result = await _sender.Send(new GetCartQuery(GetUserId()), ct);
        return Ok(result.Value);
    }

    /// <summary>
    /// افزودن یک قلم کالا با تعداد مشخص به سبد خرید
    /// </summary>
    /// <param name="request">شناسه محصول و تعداد</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>وضعیت به‌روزشده سبد خرید</returns>
    /// <response code="200">کالا به سبد اضافه شد و سبد جدید بازگردانده شد.</response>
    /// <response code="400">تعداد نامعتبر است یا موجودی انبار کافی نیست.</response>
    [HttpPost("items")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CartDto>> AddItem([FromBody] AddCartItemRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new AddItemToCartCommand(GetUserId(), request.ProductId, request.Quantity), ct);
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// تغییر و به‌روزرسانی تعداد یک کالای موجود در سبد خرید
    /// </summary>
    /// <param name="productId">شناسه محصول</param>
    /// <param name="request">تعداد جدید کالا</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>وضعیت به‌روزشده سبد خرید</returns>
    /// <response code="200">تعداد کالا در سبد تغییر کرد.</response>
    /// <response code="400">تعداد نامعتبر است یا کالا در سبد یافت نشد.</response>
    [HttpPut("items/{productId:guid}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CartDto>> UpdateQuantity(Guid productId, [FromBody] UpdateCartItemRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new UpdateCartItemQuantityCommand(GetUserId(), productId, request.Quantity), ct);
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// حذف کامل یک ردیف محصول از سبد خرید
    /// </summary>
    /// <param name="productId">شناسه محصول جهت حذف</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>وضعیت به‌روزشده سبد خرید</returns>
    /// <response code="200">کالا با موفقیت از سبد خرید حذف شد.</response>
    /// <response code="400">کالا در سبد وجود ندارد.</response>
    [HttpDelete("items/{productId:guid}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CartDto>> RemoveItem(Guid productId, CancellationToken ct)
    {
        var result = await _sender.Send(new RemoveCartItemCommand(GetUserId(), productId), ct);
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// خالی کردن و حذف تمام اقلام موجود در سبد خرید کاربر
    /// </summary>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>پیام تأیید خالی شدن سبد</returns>
    /// <response code="200">سبد خرید با موفقیت خالی شد.</response>
    [HttpDelete]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MessageResponseDto>> ClearCart(CancellationToken ct)
    {
        await _sender.Send(new ClearCartCommand(GetUserId()), ct);
        return Ok(new MessageResponseDto("سبد خرید با موفقیت خالی شد."));
    }
}
