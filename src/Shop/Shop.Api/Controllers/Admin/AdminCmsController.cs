using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Filters;
using Shop.Application.Common.Models;
using Shop.Application.DTOs;
using Shop.Domain.Entities;
using Shop.Infrastructure.Persistence;

namespace Shop.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin")]
[Authorize]
[Produces("application/json")]
public class AdminCmsController : ControllerBase
{
    private readonly ShopDbContext _db;

    public AdminCmsController(ShopDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// لیست مقالات وبلاگ با فیلتر وضعیت و صفحه‌بندی
    /// </summary>
    [HttpGet("blog/articles")]
    [RequirePermission("blog.read")]
    public async Task<IActionResult> GetArticles(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? status = "all",
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        limit = Math.Max(1, limit);

        var query = _db.BlogPosts.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status) && status != "all")
        {
            var isPublished = status == "published";
            query = query.Where(b => b.IsPublished == isPublished);
        }

        var total = await query.CountAsync(ct);
        var list = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(ct);

        var dtos = list.Select(b => new AdminBlogArticleDto(
            Id: b.Id.ToString(),
            Title: b.Title,
            Slug: b.Slug,
            Excerpt: b.Summary,
            Author: b.AuthorName,
            Category: b.Category != null ? b.Category.Name : "راهنمای استایل",
            Status: b.IsPublished ? "published" : "draft",
            CoverImage: b.CoverImageUrl ?? "https://cdn.aura-leather.ir/blog-cover.jpg",
            MetaTitle: b.Title,
            MetaDescription: b.Summary,
            FocusKeyword: "کفش رسمی چرم",
            RelatedProducts: ["prod-1", "prod-2"],
            Content: b.Content,
            CreatedAt: b.CreatedAt.ToString("yyyy/MM/dd")
        )).ToList();

        var meta = new PaginationMeta(page, limit, total);
        return Ok(ApiResponse<List<AdminBlogArticleDto>>.Ok(dtos, "لیست مقالات دریافت شد.", meta));
    }

    /// <summary>
    /// ایجاد مقاله جدید همراه با اتصال کفش‌های مکمل
    /// </summary>
    [HttpPost("blog/articles")]
    [RequirePermission("blog.create")]
    public async Task<IActionResult> CreateArticle(
        [FromBody] CreateOrUpdateBlogArticleRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new ApiErrorResponse(400, StandardErrorCodes.ValidationFailed, "عنوان مقاله الزامی است."));
        }

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? request.Title.Trim().ToLower().Replace(' ', '-')
            : request.Slug.Trim().ToLower();

        var post = BlogPost.Create(
            title: request.Title,
            slug: slug,
            summary: request.Excerpt ?? request.Title,
            content: request.Content,
            authorId: Guid.NewGuid(),
            authorName: request.Author ?? "چرم اورا",
            categoryId: null,
            categoryName: request.Category ?? "راهنمای استایل",
            coverImageUrl: request.CoverImage,
            tags: request.RelatedProducts ?? [],
            publishImmediately: request.Status == "published"
        );

        _db.BlogPosts.Add(post);
        await _db.SaveChangesAsync(ct);

        var dto = new AdminBlogArticleDto(
            post.Id.ToString(),
            post.Title,
            post.Slug,
            post.Summary,
            post.AuthorName,
            request.Category ?? "راهنمای استایل",
            post.IsPublished ? "published" : "draft",
            post.CoverImageUrl ?? "",
            request.MetaTitle ?? post.Title,
            request.MetaDescription ?? post.Summary,
            request.FocusKeyword ?? "",
            request.RelatedProducts ?? [],
            post.Content,
            post.CreatedAt.ToString("yyyy/MM/dd")
        );

        return StatusCode(201, ApiResponse<AdminBlogArticleDto>.Created(dto, "مقاله با موفقیت ایجاد شد."));
    }

    /// <summary>
    /// ویرایش مقاله وبلاگ
    /// </summary>
    [HttpPut("blog/articles/{id}")]
    [RequirePermission("blog.update")]
    public async Task<IActionResult> UpdateArticle(
        string id,
        [FromBody] CreateOrUpdateBlogArticleRequest request,
        CancellationToken ct)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            return BadRequest(new ApiErrorResponse(400, StandardErrorCodes.ValidationFailed, "شناسه نامعتبر است."));
        }

        var post = await _db.BlogPosts.FirstOrDefaultAsync(b => b.Id == guid, ct);
        if (post == null)
        {
            return NotFound(new ApiErrorResponse(404, StandardErrorCodes.NotFound, "مقاله یافت نشد."));
        }

        post.Update(
            request.Title,
            request.Slug,
            request.Excerpt ?? request.Title,
            request.Content,
            post.CategoryId,
            request.Category,
            request.CoverImage,
            request.RelatedProducts ?? [],
            request.Status == "published"
        );

        await _db.SaveChangesAsync(ct);

        var dto = new AdminBlogArticleDto(
            post.Id.ToString(),
            post.Title,
            post.Slug,
            post.Summary,
            post.AuthorName,
            request.Category ?? "راهنمای استایل",
            post.IsPublished ? "published" : "draft",
            post.CoverImageUrl ?? "",
            request.MetaTitle ?? post.Title,
            request.MetaDescription ?? post.Summary,
            request.FocusKeyword ?? "",
            request.RelatedProducts ?? [],
            post.Content,
            post.CreatedAt.ToString("yyyy/MM/dd")
        );

        return Ok(ApiResponse<AdminBlogArticleDto>.Ok(dto, "مقاله با موفقیت به‌روزرسانی شد."));
    }

    /// <summary>
    /// حذف مقاله وبلاگ
    /// </summary>
    [HttpDelete("blog/articles/{id}")]
    [RequirePermission("blog.delete")]
    public async Task<IActionResult> DeleteArticle(string id, CancellationToken ct)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            return BadRequest(new ApiErrorResponse(400, StandardErrorCodes.ValidationFailed, "شناسه نامعتبر است."));
        }

        var post = await _db.BlogPosts.FirstOrDefaultAsync(b => b.Id == guid, ct);
        if (post == null)
        {
            return NotFound(new ApiErrorResponse(404, StandardErrorCodes.NotFound, "مقاله یافت نشد."));
        }

        _db.BlogPosts.Remove(post);
        await _db.SaveChangesAsync(ct);

        return Ok(ApiResponse<object>.Ok(null, "مقاله با موفقیت حذف گردید."));
    }

    /// <summary>
    /// دریافت ترتیب و وضعیت بلوک‌های صفحه نخست فروشگاه
    /// </summary>
    [HttpGet("cms/homepage/blocks")]
    [RequirePermission("cms.read")]
    public async Task<IActionResult> GetHomepageBlocks(CancellationToken ct)
    {
        var blocks = await _db.CmsHomepageBlocks.AsNoTracking().OrderBy(b => b.DisplayOrder).ToListAsync(ct);
        var dtos = blocks.Select(b => new CmsHomepageBlockDto(
            b.BlockId,
            b.Type,
            b.IsVisible,
            b.DisplayOrder
        )).ToList();

        return Ok(ApiResponse<object>.Ok(new { blocks = dtos }));
    }

    /// <summary>
    /// ذخیره چیدمان و فعال/غیرفعال بودن بلوک‌های صفحه نخست
    /// </summary>
    [HttpPut("cms/homepage/blocks")]
    [RequirePermission("cms.update")]
    public async Task<IActionResult> UpdateHomepageBlocks(
        [FromBody] UpdateHomepageBlocksRequest request,
        CancellationToken ct)
    {
        if (request.Blocks != null && request.Blocks.Count > 0)
        {
            var existing = await _db.CmsHomepageBlocks.ToListAsync(ct);
            foreach (var reqBlock in request.Blocks)
            {
                var block = existing.FirstOrDefault(b => b.BlockId == reqBlock.Id);
                if (block != null)
                {
                    block.Update(reqBlock.IsVisible, reqBlock.Order);
                }
                else
                {
                    _db.CmsHomepageBlocks.Add(new CmsHomepageBlock(reqBlock.Id, reqBlock.Type, reqBlock.IsVisible, reqBlock.Order));
                }
            }
            await _db.SaveChangesAsync(ct);
        }

        return Ok(ApiResponse<object>.Ok(null, "چیدمان بلوک‌های صفحه نخست با موفقیت به‌روزرسانی شد."));
    }
}
