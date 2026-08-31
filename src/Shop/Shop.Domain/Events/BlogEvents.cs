using Shop.Domain.Common;

namespace Shop.Domain.Events;

public record BlogPostCreatedEvent(
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
) : IDomainEvent;

public record BlogPostUpdatedEvent(
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
) : IDomainEvent;

public record BlogPostDeletedEvent(Guid PostId) : IDomainEvent;

public record BlogCommentAddedEvent(
    Guid CommentId,
    Guid PostId,
    Guid? UserId,
    string UserName,
    string Content,
    bool IsApproved,
    DateTimeOffset CreatedAt
) : IDomainEvent;
