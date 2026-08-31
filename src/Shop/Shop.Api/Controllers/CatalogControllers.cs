using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.DTOs;
using Shop.Application.Features.Catalog.Commands;
using Shop.Application.Features.Catalog.Queries;

namespace Shop.Api.Controllers;

/// <summary>
/// سرویس‌های مدیریت و مشاهده دسته‌بندی‌های محصولات
/// </summary>
[ApiController]
[Route("api/categories")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly ISender _sender;

    public CategoriesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// دریافت ساختار درختی و لیست کامل تمام دسته‌بندی‌های فعال
    /// </summary>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>لیست دسته‌بندی‌های محصولات</returns>
    /// <response code="200">لیست دسته‌بندی‌ها با موفقیت بازگردانده شد.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CategoryDto>>> GetAll(CancellationToken ct)
    {
        var result = await _sender.Send(new GetCategoriesQuery(), ct);
        return Ok(result);
    }

    /// <summary>
    /// ایجاد دسته‌بندی جدید برای محصولات (مخصوص مدیر سیستم)
    /// </summary>
    /// <param name="command">مشخصات دسته‌بندی شامل نام، نامک (slug) و دسته‌بندی والد</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>شناسه یکتای دسته‌بندی ایجاد شده</returns>
    /// <response code="201">دسته‌بندی با موفقیت ایجاد شد.</response>
    /// <response code="400">اطلاعات دسته‌بندی نامعتبر است یا نامک تکراری است.</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(EntityIdResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EntityIdResponseDto>> Create([FromBody] CreateCategoryCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.Error.Code, message = result.Error.Description });
        }

        return CreatedAtAction(nameof(GetAll), new { id = result.Value }, new EntityIdResponseDto(result.Value));
    }
}

/// <summary>
/// سرویس‌های کاتالوگ محصولات، جستجو، فیلتر و مدیریت کالاها
/// </summary>
[ApiController]
[Route("api/products")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// جستجو و فیلتر پیشرفته محصولات با صفحه‌بندی (سمت خواندن MongoDB)
    /// </summary>
    /// <param name="query">پارامترهای فیلتر شامل شناسه دسته‌بندی، عبارت جستجو، محدوده قیمت و شماره صفحه</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>لیست صفحه‌بندی شده محصولات منطبق با فیلتر</returns>
    /// <response code="200">نتایج جستجو با موفقیت بازگردانده شد.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<ProductDto>>> GetProducts([FromQuery] GetProductsQuery query, CancellationToken ct)
    {
        var result = await _sender.Send(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// دریافت اطلاعات کامل یک محصول بر اساس شناسه یکتا (GUID)
    /// </summary>
    /// <param name="id">شناسه یکتای محصول</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>مشخصات کامل کالا</returns>
    /// <response code="200">محصول یافت شد و اطلاعات آن بازگردانده شد.</response>
    /// <response code="404">محصولی با این شناسه یافت نشد.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetProductByIdQuery(id), ct);
        if (result.IsFailure)
        {
            return NotFound(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// دریافت اطلاعات محصول بر اساس نامک سئو (Slug)
    /// </summary>
    /// <param name="slug">نامک یکتای محصول</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>مشخصات کامل کالا</returns>
    /// <response code="200">محصول یافت شد.</response>
    /// <response code="404">محصولی با این نامک یافت نشد.</response>
    [HttpGet("slug/{slug}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetBySlug(string slug, CancellationToken ct)
    {
        var result = await _sender.Send(new GetProductBySlugQuery(slug), ct);
        if (result.IsFailure)
        {
            return NotFound(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// ایجاد محصول جدید و انتشار رویداد همگام‌سازی CQRS با RabbitMQ (مخصوص مدیر سیستم)
    /// </summary>
    /// <param name="command">مشخصات کامل محصول شامل قیمت، موجودی، تصاویر و دسته‌بندی</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>شناسه محصول جدید</returns>
    /// <response code="201">محصول جدید با موفقیت ایجاد شد.</response>
    /// <response code="400">اطلاعات محصول نامعتبر است یا کد کالا (SKU) تکراری است.</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(EntityIdResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EntityIdResponseDto>> CreateProduct([FromBody] CreateProductCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.Error.Code, message = result.Error.Description });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value }, new EntityIdResponseDto(result.Value));
    }

    /// <summary>
    /// به‌روزرسانی موجودی انبار محصول (مخصوص مدیر سیستم)
    /// </summary>
    /// <param name="id">شناسه یکتای محصول</param>
    /// <param name="newStock">تعداد جدید موجودی انبار</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>پیام تأیید به‌روزرسانی موجودی</returns>
    /// <response code="200">موجودی انبار با موفقیت به‌روزرسانی شد.</response>
    /// <response code="400">مقدار موجودی نامعتبر است یا محصول یافت نشد.</response>
    [HttpPatch("{id:guid}/stock")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MessageResponseDto>> UpdateStock(Guid id, [FromBody] int newStock, CancellationToken ct)
    {
        var result = await _sender.Send(new UpdateProductStockCommand(id, newStock), ct);
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(new MessageResponseDto("موجودی انبار با موفقیت به‌روزرسانی شد."));
    }
}
