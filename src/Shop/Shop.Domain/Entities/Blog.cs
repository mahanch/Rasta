using Shop.Domain.Common;
using Shop.Domain.Events;

namespace Shop.Domain.Entities;

public class BlogCategory : Entity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private BlogCategory() { }

    public BlogCategory(string name, string slug, string? description = null)
    {
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Description = description;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string name, string slug, string? description)
    {
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Description = description;
    }
}

public class BlogPost : AggregateRoot<Guid>
{
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Summary { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string? CoverImageUrl { get; private set; }
    
    public Guid AuthorId { get; private set; }
    public string AuthorName { get; private set; } = string.Empty;

    public Guid? CategoryId { get; private set; }
    public BlogCategory? Category { get; private set; }

    public List<string> Tags { get; private set; } = [];
    public int ReadingTimeMinutes { get; private set; } = 3;
    public int ViewCount { get; private set; } = 0;

    public bool IsPublished { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; private set; }

    public List<BlogComment> Comments { get; private set; } = [];

    private BlogPost() { }

    public static BlogPost Create(
        string title,
        string slug,
        string summary,
        string content,
        Guid authorId,
        string authorName,
        Guid? categoryId = null,
        string? categoryName = null,
        string? coverImageUrl = null,
        List<string>? tags = null,
        bool publishImmediately = true)
    {
        var post = new BlogPost
        {
            Title = title.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            Summary = summary.Trim(),
            Content = content,
            AuthorId = authorId,
            AuthorName = authorName,
            CategoryId = categoryId,
            CoverImageUrl = coverImageUrl,
            Tags = tags ?? [],
            ReadingTimeMinutes = CalculateReadingTime(content),
            IsPublished = publishImmediately,
            PublishedAt = publishImmediately ? DateTimeOffset.UtcNow : null,
            CreatedAt = DateTimeOffset.UtcNow
        };

        post.AddDomainEvent(new BlogPostCreatedEvent(
            post.Id,
            post.Title,
            post.Slug,
            post.Summary,
            post.Content,
            post.CoverImageUrl,
            post.AuthorId,
            post.AuthorName,
            post.CategoryId,
            categoryName,
            post.Tags,
            post.ReadingTimeMinutes,
            post.IsPublished,
            post.PublishedAt,
            post.CreatedAt
        ));

        return post;
    }

    public void Update(
        string title,
        string slug,
        string summary,
        string content,
        Guid? categoryId,
        string? categoryName,
        string? coverImageUrl,
        List<string>? tags,
        bool isPublished)
    {
        Title = title.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Summary = summary.Trim();
        Content = content;
        CategoryId = categoryId;
        CoverImageUrl = coverImageUrl;
        Tags = tags ?? [];
        ReadingTimeMinutes = CalculateReadingTime(content);

        if (!IsPublished && isPublished)
        {
            PublishedAt = DateTimeOffset.UtcNow;
        }

        IsPublished = isPublished;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new BlogPostUpdatedEvent(
            Id,
            Title,
            Slug,
            Summary,
            Content,
            CoverImageUrl,
            CategoryId,
            categoryName,
            Tags,
            ReadingTimeMinutes,
            IsPublished,
            PublishedAt,
            UpdatedAt.Value
        ));
    }

    public void Publish()
    {
        if (!IsPublished)
        {
            IsPublished = true;
            PublishedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public void Unpublish()
    {
        IsPublished = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void IncrementViewCount()
    {
        ViewCount++;
    }

    public BlogComment AddComment(Guid? userId, string userName, string userEmail, string content, bool autoApprove = false)
    {
        var comment = new BlogComment(Id, userId, userName, userEmail, content, autoApprove);
        Comments.Add(comment);

        AddDomainEvent(new BlogCommentAddedEvent(
            comment.Id,
            Id,
            userId,
            userName,
            content,
            comment.IsApproved,
            comment.CreatedAt
        ));

        return comment;
    }

    public void ApproveComment(Guid commentId)
    {
        var comment = Comments.FirstOrDefault(c => c.Id == commentId);
        comment?.Approve();
    }

    private static int CalculateReadingTime(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 1;
        var words = text.Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;
        return Math.Max(1, (int)Math.Ceiling(words / 200.0));
    }
}

public class BlogComment : Entity<Guid>
{
    public Guid BlogPostId { get; private set; }
    public Guid? UserId { get; private set; }
    public string UserName { get; private set; } = string.Empty;
    public string UserEmail { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public bool IsApproved { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private BlogComment() { }

    public BlogComment(Guid blogPostId, Guid? userId, string userName, string userEmail, string content, bool isApproved = false)
    {
        BlogPostId = blogPostId;
        UserId = userId;
        UserName = userName.Trim();
        UserEmail = userEmail.Trim().ToLowerInvariant();
        Content = content.Trim();
        IsApproved = isApproved;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Approve()
    {
        IsApproved = true;
    }
}
