namespace Shop.Application.DTOs;

public record BlogPostSummaryDto(
    Guid Id,
    string Title,
    string Slug,
    string Summary,
    string? CoverImageUrl,
    string AuthorName,
    Guid? CategoryId,
    string? CategoryName,
    List<string> Tags,
    int ReadingTimeMinutes,
    int ViewCount,
    int CommentCount,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt
);

public record BlogPostDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string Summary,
    string Content,
    string? CoverImageUrl,
    Guid AuthorId,
    string AuthorName,
    Guid? CategoryId,
    string? CategoryName,
    List<string> Tags,
    int ReadingTimeMinutes,
    int ViewCount,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt,
    List<BlogCommentDto> Comments
);

public record BlogCommentDto(
    Guid Id,
    Guid? UserId,
    string UserName,
    string Content,
    bool IsApproved,
    DateTimeOffset CreatedAt
);

public record BlogCategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    DateTimeOffset CreatedAt
);
