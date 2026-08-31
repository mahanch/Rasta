namespace Shop.Application.Contracts;

public record BlogPostCreatedIntegrationEvent(
    Guid PostId,
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
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt
);

public record BlogPostUpdatedIntegrationEvent(
    Guid PostId,
    string Title,
    string Slug,
    string Summary,
    string Content,
    string? CoverImageUrl,
    Guid? CategoryId,
    string? CategoryName,
    List<string> Tags,
    int ReadingTimeMinutes,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    DateTimeOffset UpdatedAt
);

public record BlogPostDeletedIntegrationEvent(Guid PostId);

public record BlogCommentAddedIntegrationEvent(
    Guid CommentId,
    Guid PostId,
    Guid? UserId,
    string UserName,
    string Content,
    bool IsApproved,
    DateTimeOffset CreatedAt
);

public record BlogCommentApprovedIntegrationEvent(Guid PostId, Guid CommentId);

public record BlogCategoryCreatedIntegrationEvent(
    Guid CategoryId,
    string Name,
    string Slug,
    string? Description,
    DateTimeOffset CreatedAt
);
