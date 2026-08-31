using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Shop.Application.ReadModels;

public class BlogPostReadModel
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }

    public List<string> Tags { get; set; } = [];
    public int ReadingTimeMinutes { get; set; } = 3;
    public int ViewCount { get; set; } = 0;

    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public List<BlogCommentReadModel> Comments { get; set; } = [];
    public int ApprovedCommentCount => Comments.Count(c => c.IsApproved);
}

public class BlogCommentReadModel
{
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid? UserId { get; set; }

    public string UserName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class BlogCategoryReadModel
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
