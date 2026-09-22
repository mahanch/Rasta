using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Application.DTOs;
using Shop.Domain.Common;

namespace Shop.Application.Features.Blog.Queries;

public record GetBlogPostsQuery(
    string? Search = null,
    Guid? CategoryId = null,
    string? Tag = null,
    bool IncludeUnpublished = false,
    int Page = 1,
    int PageSize = 10
) : IRequest<PaginatedResult<BlogPostSummaryDto>>;

public class GetBlogPostsQueryHandler : IRequestHandler<GetBlogPostsQuery, PaginatedResult<BlogPostSummaryDto>>
{
    private readonly IShopDbContext _db;

    public GetBlogPostsQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<PaginatedResult<BlogPostSummaryDto>> Handle(GetBlogPostsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.BlogPosts
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Comments)
            .AsQueryable();

        if (!request.IncludeUnpublished)
        {
            query = query.Where(p => p.IsPublished);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(p =>
                p.Title.ToLower().Contains(search) ||
                p.Summary.ToLower().Contains(search) ||
                p.Content.ToLower().Contains(search));
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Tag))
        {
            var tag = request.Tag.Trim().ToLowerInvariant();
            query = query.Where(p => p.Tags.Contains(tag));
        }

        var total = await query.CountAsync(cancellationToken);
        var skip = Math.Max(0, (request.Page - 1) * request.PageSize);
        var posts = await query
            .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
            .Skip(skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = posts.Select(p => new BlogPostSummaryDto(
            p.Id,
            p.Title,
            p.Slug,
            p.Summary,
            p.CoverImageUrl,
            p.AuthorName,
            p.CategoryId,
            p.Category?.Name,
            p.Tags,
            p.ReadingTimeMinutes,
            p.ViewCount,
            p.Comments.Count(c => c.IsApproved),
            p.IsPublished,
            p.PublishedAt,
            p.CreatedAt
        )).ToList();

        var totalPages = (int)Math.Ceiling((double)total / request.PageSize);
        return new PaginatedResult<BlogPostSummaryDto>(dtos, total, request.Page, request.PageSize, totalPages);
    }
}

public record GetBlogPostBySlugQuery(string Slug, bool IncludeUnapprovedComments = false) : IRequest<Result<BlogPostDetailDto>>;

public class GetBlogPostBySlugQueryHandler : IRequestHandler<GetBlogPostBySlugQuery, Result<BlogPostDetailDto>>
{
    private readonly IShopDbContext _db;

    public GetBlogPostBySlugQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<BlogPostDetailDto>> Handle(GetBlogPostBySlugQuery request, CancellationToken cancellationToken)
    {
        var slug = request.Slug.ToLowerInvariant();
        var post = await _db.BlogPosts
            .Include(p => p.Category)
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);

        if (post == null)
        {
            return Result<BlogPostDetailDto>.Failure(new Error("Blog.NotFound", "Blog post not found."));
        }

        // Increment view count in PostgreSQL
        post.IncrementViewCount();
        await _db.SaveChangesAsync(cancellationToken);

        var comments = post.Comments
            .Where(c => request.IncludeUnapprovedComments || c.IsApproved)
            .Select(c => new BlogCommentDto(c.Id, c.UserId, c.UserName, c.Content, c.IsApproved, c.CreatedAt))
            .ToList();

        return Result<BlogPostDetailDto>.Success(new BlogPostDetailDto(
            post.Id,
            post.Title,
            post.Slug,
            post.Summary,
            post.Content,
            post.CoverImageUrl,
            post.AuthorId,
            post.AuthorName,
            post.CategoryId,
            post.Category?.Name,
            post.Tags,
            post.ReadingTimeMinutes,
            post.ViewCount,
            post.IsPublished,
            post.PublishedAt,
            post.CreatedAt,
            comments
        ));
    }
}

public record GetBlogCategoriesQuery() : IRequest<List<BlogCategoryDto>>;

public class GetBlogCategoriesQueryHandler : IRequestHandler<GetBlogCategoriesQuery, List<BlogCategoryDto>>
{
    private readonly IShopDbContext _db;

    public GetBlogCategoriesQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<List<BlogCategoryDto>> Handle(GetBlogCategoriesQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.BlogCategories
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return list.Select(c => new BlogCategoryDto(c.Id, c.Name, c.Slug, c.Description, c.CreatedAt)).ToList();
    }
}
