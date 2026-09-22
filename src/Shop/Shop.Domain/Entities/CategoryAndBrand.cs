using Shop.Domain.Common;
using Shop.Domain.Events;

namespace Shop.Domain.Entities;

public class Category : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? ImageUrl { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Guid? ParentCategoryId { get; private set; }
    public Category? ParentCategory { get; private set; }
    public List<Category> SubCategories { get; private set; } = [];
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private Category() { }

    public Category(string name, string slug, string? description = null, string? imageUrl = null, Guid? parentCategoryId = null)
    {
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Description = description;
        ImageUrl = imageUrl;
        ParentCategoryId = parentCategoryId;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new CategoryCreatedOrUpdatedEvent(Id, Name, Slug, Description, ImageUrl, IsActive, ParentCategoryId));
    }

    public void Update(string name, string slug, string? description, string? imageUrl, bool isActive, Guid? parentCategoryId)
    {
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Description = description;
        ImageUrl = imageUrl;
        IsActive = isActive;
        ParentCategoryId = parentCategoryId;

        AddDomainEvent(new CategoryCreatedOrUpdatedEvent(Id, Name, Slug, Description, ImageUrl, IsActive, ParentCategoryId));
    }
}

public class Brand : Entity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? LogoUrl { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private Brand() { }

    public Brand(string name, string slug, string? logoUrl = null)
    {
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        LogoUrl = logoUrl;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string name, string slug, string? logoUrl)
    {
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        LogoUrl = logoUrl;
    }
}
