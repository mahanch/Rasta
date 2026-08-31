using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.DTOs;
using Shop.Application.Features.Blog.Commands;
using Shop.Application.Features.Blog.Queries;

namespace Shop.Api.Controllers;

/// <summary>
/// مدل درخواست ایجاد مقاله جدید در وبلاگ
/// </summary>
/// <param name="Title">عنوان مقاله</param>
/// <param name="Slug">نامک یکتا برای آدرس اینترنتی (SEO Slug)</param>
/// <param name="Summary">خلاصه یا چکیده کوتاه مقاله</param>
/// <param name="Content">متن اصلی مقاله (پشتیبانی از Markdown یا HTML)</param>
/// <param name="CategoryId">شناسه دسته‌بندی وبلاگ (اختیاری)</param>
/// <param name="CoverImageUrl">آدرس تصویر شاخص مقاله</param>
/// <param name="Tags">برچسب‌ها و تگ‌های مقاله</param>
/// <param name="PublishImmediately">انتشار فوری مقاله در صورت true بودن</param>
public record CreateBlogPostRequest(
    string Title,
    string Slug,
    string Summary,
    string Content,
    Guid? CategoryId,
    string? CoverImageUrl,
    List<string>? Tags,
    bool PublishImmediately = true
);

/// <summary>
/// مدل درخواست ویرایش مقاله موجود در وبلاگ
/// </summary>
/// <param name="Title">عنوان جدید مقاله</param>
/// <param name="Slug">نامک جدید مقاله</param>
/// <param name="Summary">خلاصه جدید مقاله</param>
/// <param name="Content">متن جدید مقاله</param>
/// <param name="CategoryId">شناسه دسته‌بندی جدید</param>
/// <param name="CoverImageUrl">آدرس تصویر شاخص جدید</param>
/// <param name="Tags">لیست جدید برچسب‌ها</param>
/// <param name="IsPublished">وضعیت انتشار مقاله</param>
public record UpdateBlogPostRequest(
    string Title,
    string Slug,
    string Summary,
    string Content,
    Guid? CategoryId,
    string? CoverImageUrl,
    List<string>? Tags,
    bool IsPublished
);

/// <summary>
/// مدل درخواست تغییر وضعیت انتشار مقاله
/// </summary>
/// <param name="Publish">در صورت true منتشر می‌شود و در صورت false پیش‌نویس می‌گردد</param>
public record PublishPostRequest(bool Publish);

/// <summary>
/// مدل درخواست ثبت دیدگاه جدید برای مقاله
/// </summary>
/// <param name="UserName">نام ارسال‌کننده دیدگاه</param>
/// <param name="UserEmail">ایمیل ارسال‌کننده دیدگاه</param>
/// <param name="Content">متن دیدگاه</param>
public record AddCommentRequest(string UserName, string UserEmail, string Content);

/// <summary>
/// مدل درخواست ایجاد دسته‌بندی جدید در وبلاگ
/// </summary>
/// <param name="Name">نام دسته‌بندی</param>
/// <param name="Slug">نامک یکتای دسته‌بندی</param>
/// <param name="Description">توضیحات اختیاری دسته‌بندی</param>
public record CreateBlogCategoryRequest(string Name, string Slug, string? Description);

/// <summary>
/// سرویس‌های مدیریت وبلاگ، مقالات، دسته‌بندی‌ها و نظرات کاربران
/// </summary>
[ApiController]
[Route("api/blog")]
[Produces("application/json")]
public class BlogController : ControllerBase
{
    private readonly ISender _sender;

    public BlogController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// دریافت لیست مقالات وبلاگ با امکان جستجو، فیلتر بر اساس دسته‌بندی و برچسب، و صفحه‌بندی
    /// </summary>
    /// <param name="search">عبارت جستجو در عنوان و محتوا</param>
    /// <param name="categoryId">فیلتر بر اساس شناسه دسته‌بندی</param>
    /// <param name="tag">فیلتر بر اساس برچسب</param>
    /// <param name="page">شماره صفحه (پیش‌فرض ۱)</param>
    /// <param name="pageSize">تعداد در هر صفحه (پیش‌فرض ۱۰)</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>لیست صفحه‌بندی شده مقالات وبلاگ</returns>
    /// <response code="200">لیست مقالات با موفقیت بازگردانده شد.</response>
    [HttpGet("posts")]
    [ProducesResponseType(typeof(PaginatedResult<BlogPostSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<BlogPostSummaryDto>>> GetPosts(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? tag,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var isAdmin = User.IsInRole("Admin");
        var result = await _sender.Send(new GetBlogPostsQuery(search, categoryId, tag, isAdmin, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// دریافت محتوای کامل یک مقاله وبلاگ به همراه نظرات تأیید شده بر اساس نامک (Slug)
    /// </summary>
    /// <param name="slug">نامک اختصاصی مقاله</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>اطلاعات کامل مقاله و دیدگاه‌ها</returns>
    /// <response code="200">مقاله با موفقیت یافت شد.</response>
    /// <response code="404">مقاله مورد نظر یافت نشد.</response>
    [HttpGet("posts/{slug}")]
    [ProducesResponseType(typeof(BlogPostDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BlogPostDetailDto>> GetPostBySlug(string slug, CancellationToken ct)
    {
        var isAdmin = User.IsInRole("Admin");
        var result = await _sender.Send(new GetBlogPostBySlugQuery(slug, isAdmin), ct);
        if (result.IsFailure)
        {
            return NotFound(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// دریافت لیست کامل دسته‌بندی‌های وبلاگ
    /// </summary>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>لیست دسته‌بندی‌های وبلاگ</returns>
    /// <response code="200">لیست دسته‌بندی‌ها با موفقیت بازگردانده شد.</response>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(List<BlogCategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BlogCategoryDto>>> GetCategories(CancellationToken ct)
    {
        var result = await _sender.Send(new GetBlogCategoriesQuery(), ct);
        return Ok(result);
    }

    /// <summary>
    /// ایجاد و نگارش مقاله جدید در وبلاگ (مخصوص مدیر سیستم)
    /// </summary>
    /// <param name="request">مشخصات کامل مقاله</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>شناسه یکتای مقاله ایجاد شده</returns>
    /// <response code="201">مقاله با موفقیت ایجاد شد.</response>
    /// <response code="400">اطلاعات مقاله نامعتبر است یا نامک تکراری است.</response>
    [HttpPost("posts")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(EntityIdResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EntityIdResponseDto>> CreatePost([FromBody] CreateBlogPostRequest request, CancellationToken ct)
    {
        var authorId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var authorName = User.FindFirstValue(ClaimTypes.Name) ?? "Admin";

        var command = new CreateBlogPostCommand(
            request.Title,
            request.Slug,
            request.Summary,
            request.Content,
            authorId,
            authorName,
            request.CategoryId,
            request.CoverImageUrl,
            request.Tags,
            request.PublishImmediately
        );

        var result = await _sender.Send(command, ct);
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.Error.Code, message = result.Error.Description });
        }

        return CreatedAtAction(nameof(GetPostBySlug), new { slug = request.Slug }, new EntityIdResponseDto(result.Value));
    }

    /// <summary>
    /// ویرایش محتوا و اطلاعات مقاله وبلاگ (مخصوص مدیر سیستم)
    /// </summary>
    /// <param name="id">شناسه یکتای مقاله</param>
    /// <param name="request">اطلاعات به‌روزشده مقاله</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>پیام تأیید ویرایش مقاله</returns>
    /// <response code="200">مقاله با موفقیت ویرایش شد.</response>
    /// <response code="400">اطلاعات نامعتبر است یا مقاله یافت نشد.</response>
    [HttpPut("posts/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MessageResponseDto>> UpdatePost(Guid id, [FromBody] UpdateBlogPostRequest request, CancellationToken ct)
    {
        var command = new UpdateBlogPostCommand(
            id,
            request.Title,
            request.Slug,
            request.Summary,
            request.Content,
            request.CategoryId,
            request.CoverImageUrl,
            request.Tags,
            request.IsPublished
        );

        var result = await _sender.Send(command, ct);
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(new MessageResponseDto("مقاله وبلاگ با موفقیت به‌روزرسانی شد."));
    }

    /// <summary>
    /// تغییر وضعیت انتشار مقاله به صورت منتشرشده یا پیش‌نویس (مخصوص مدیر سیستم)
    /// </summary>
    /// <param name="id">شناسه یکتای مقاله</param>
    /// <param name="request">وضعیت انتشار مورد نظر</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>پیام تأیید تغییر وضعیت انتشار</returns>
    /// <response code="200">وضعیت انتشار با موفقیت تغییر کرد.</response>
    /// <response code="400">شناسه مقاله نامعتبر است یا مقاله یافت نشد.</response>
    [HttpPatch("posts/{id:guid}/publish")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MessageResponseDto>> PublishPost(Guid id, [FromBody] PublishPostRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new PublishBlogPostCommand(id, request.Publish), ct);
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(new MessageResponseDto($"وضعیت انتشار مقاله با موفقیت به {(request.Publish ? "منتشر شده" : "پیش‌نویس")} تغییر یافت."));
    }

    /// <summary>
    /// حذف یک مقاله از وبلاگ (مخصوص مدیر سیستم)
    /// </summary>
    /// <param name="id">شناسه یکتای مقاله</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>پیام تأیید حذف مقاله</returns>
    /// <response code="200">مقاله با موفقیت حذف شد.</response>
    /// <response code="400">مقاله یافت نشد.</response>
    [HttpDelete("posts/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MessageResponseDto>> DeletePost(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new DeleteBlogPostCommand(id), ct);
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(new MessageResponseDto("مقاله با موفقیت حذف شد."));
    }

    /// <summary>
    /// ثبت نظر جدید برای یک مقاله وبلاگ (نظرات پس از تأیید مدیر نمایش داده می‌شوند)
    /// </summary>
    /// <param name="postId">شناسه مقاله مورد نظر</param>
    /// <param name="request">مشخصات و متن نظر کاربر</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>شناسه نظر ثبت‌شده و پیام تأیید</returns>
    /// <response code="200">نظر با موفقیت ثبت شد و پس از بررسی منتشر خواهد شد.</response>
    /// <response code="400">اطلاعات نظر نامعتبر است یا مقاله یافت نشد.</response>
    [HttpPost("posts/{postId:guid}/comments")]
    [ProducesResponseType(typeof(CommentCreatedResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CommentCreatedResponseDto>> AddComment(Guid postId, [FromBody] AddCommentRequest request, CancellationToken ct)
    {
        Guid? userId = null;
        if (User.Identity?.IsAuthenticated == true && Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid))
        {
            userId = uid;
        }

        var command = new AddBlogCommentCommand(postId, userId, request.UserName, request.UserEmail, request.Content);
        var result = await _sender.Send(command, ct);
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(new CommentCreatedResponseDto(result.Value, "دیدگاه شما با موفقیت ثبت شد و پس از تأیید مدیر نمایش داده خواهد شد."));
    }

    /// <summary>
    /// تأیید دیدگاه کاربران برای نمایش عمومی در وبلاگ (مخصوص مدیر سیستم)
    /// </summary>
    /// <param name="postId">شناسه مقاله</param>
    /// <param name="commentId">شناسه دیدگاه</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>پیام تأیید</returns>
    /// <response code="200">دیدگاه با موفقیت تأیید شد.</response>
    /// <response code="400">شناسه دیدگاه یا مقاله نامعتبر است.</response>
    [HttpPatch("posts/{postId:guid}/comments/{commentId:guid}/approve")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MessageResponseDto>> ApproveComment(Guid postId, Guid commentId, CancellationToken ct)
    {
        var result = await _sender.Send(new ApproveBlogCommentCommand(postId, commentId), ct);
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(new MessageResponseDto("دیدگاه با موفقیت تأیید شد."));
    }

    /// <summary>
    /// ایجاد دسته‌بندی جدید برای مقالات وبلاگ (مخصوص مدیر سیستم)
    /// </summary>
    /// <param name="request">مشخصات دسته‌بندی وبلاگ شامل نام و نامک</param>
    /// <param name="ct">توکن لغو عملیات</param>
    /// <returns>شناسه دسته‌بندی ایجاد شده</returns>
    /// <response code="200">دسته‌بندی وبلاگ با موفقیت ایجاد شد.</response>
    /// <response code="400">اطلاعات نامعتبر است یا نامک تکراری است.</response>
    [HttpPost("categories")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CategoryCreatedResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CategoryCreatedResponseDto>> CreateCategory([FromBody] CreateBlogCategoryRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new CreateBlogCategoryCommand(request.Name, request.Slug, request.Description), ct);
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.Error.Code, message = result.Error.Description });
        }

        return Ok(new CategoryCreatedResponseDto(result.Value, "دسته‌بندی وبلاگ با موفقیت ایجاد شد."));
    }
}
