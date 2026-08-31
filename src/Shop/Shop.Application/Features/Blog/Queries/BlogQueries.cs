using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;
using Shop.Application.Common.Interfaces;
using Shop.Application.DTOs;
using Shop.Application.ReadModels;
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
    private readonly IMongoReadDbContext _mongo;

    public GetBlogPostsQueryHandler(IMongoReadDbContext mongo)
    {
        _mongo = mongo;
    }

    public async Task<PaginatedResult<BlogPostSummaryDto>> Handle(GetBlogPostsQuery request, CancellationToken cancellationToken)
    {
        var collection = _mongo.GetCollection<BlogPostReadModel>("blog_posts_view");
        var builder = Builders<BlogPostReadModel>.Filter;
        var filter = builder.Empty;

        if (!request.IncludeUnpublished)
        {
            filter &= builder.Eq(p => p.IsPublished, true);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchRegex = new BsonRegularExpression(request.Search, "i");
            filter &= (builder.Regex(p => p.Title, searchRegex) | builder.Regex(p => p.Summary, searchRegex) | builder.Regex(p => p.Content, searchRegex));
        }

        if (request.CategoryId.HasValue)
        {
            filter &= builder.Eq(p => p.CategoryId, request.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Tag))
        {
            filter &= builder.AnyEq(p => p.Tags, request.Tag.Trim().ToLowerInvariant());
        }

        var total = await collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        var skip = Math.Max(0, (request.Page - 1) * request.PageSize);
        var posts = await collection.Find(filter)
            .SortByDescending(p => p.PublishedAt ?? p.CreatedAt)
            .Skip(skip)
            .Limit(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = posts.Select(p => new BlogPostSummaryDto(
            p.Id,
            p.Title,
            p.Slug,
            p.Summary,
            p.CoverImageUrl,
            p.AuthorName,
            p.CategoryId,
            p.CategoryName,
            p.Tags,
            p.ReadingTimeMinutes,
            p.ViewCount,
            p.ApprovedCommentCount,
            p.IsPublished,
            p.PublishedAt,
            p.CreatedAt
        )).ToList();

        var totalPages = (int)Math.Ceiling((double)total / request.PageSize);
        return new PaginatedResult<BlogPostSummaryDto>(dtos, (int)total, request.Page, request.PageSize, totalPages);
    }
}

public record GetBlogPostBySlugQuery(string Slug, bool IncludeUnapprovedComments = false) : IRequest<Result<BlogPostDetailDto>>;

public class GetBlogPostBySlugQueryHandler : IRequestHandler<GetBlogPostBySlugQuery, Result<BlogPostDetailDto>>
{
    private readonly IMongoReadDbContext _mongo;

    public GetBlogPostBySlugQueryHandler(IMongoReadDbContext mongo)
    {
        _mongo = mongo;
    }

    public async Task<Result<BlogPostDetailDto>> Handle(GetBlogPostBySlugQuery request, CancellationToken cancellationToken)
    {
        var collection = _mongo.GetCollection<BlogPostReadModel>("blog_posts_view");
        var post = await collection.Find(p => p.Slug == request.Slug.ToLowerInvariant()).FirstOrDefaultAsync(cancellationToken);

        if (post == null)
        {
            return Result<BlogPostDetailDto>.Failure(new Error("Blog.NotFound", "Blog post not found."));
        }

        // Increment view count in MongoDB read model
        var update = Builders<BlogPostReadModel>.Update.Inc(p => p.ViewCount, 1);
        await collection.UpdateOneAsync(p => p.Id == post.Id, update, cancellationToken: cancellationToken);

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
            post.CategoryName,
            post.Tags,
            post.ReadingTimeMinutes,
            post.ViewCount + 1,
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
    private readonly IMongoReadDbContext _mongo;

    public GetBlogCategoriesQueryHandler(IMongoReadDbContext mongo)
    {
        _mongo = mongo;
    }

    public async Task<List<BlogCategoryDto>> Handle(GetBlogCategoriesQuery request, CancellationToken cancellationToken)
    {
        var collection = _mongo.GetCollection<BlogCategoryReadModel>("blog_categories_view");
        var list = await collection.Find(Builders<BlogCategoryReadModel>.Filter.Empty).ToListAsync(cancellationToken);
        return list.Select(c => new BlogCategoryDto(c.Id, c.Name, c.Slug, c.Description, c.CreatedAt)).ToList();
    }
}
