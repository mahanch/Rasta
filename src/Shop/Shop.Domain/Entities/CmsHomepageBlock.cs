using Shop.Domain.Common;

namespace Shop.Domain.Entities;

public class CmsHomepageBlock : Entity<Guid>
{
    public string BlockId { get; private set; } = string.Empty; // e.g. "b-hero", "b-collections"
    public string Type { get; private set; } = string.Empty; // "hero", "category_grid", "product_carousel"
    public bool IsVisible { get; private set; } = true;
    public int DisplayOrder { get; private set; }

    private CmsHomepageBlock() { }

    public CmsHomepageBlock(string blockId, string type, bool isVisible, int displayOrder, Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        BlockId = blockId;
        Type = type;
        IsVisible = isVisible;
        DisplayOrder = displayOrder;
    }

    public void Update(bool isVisible, int displayOrder)
    {
        IsVisible = isVisible;
        DisplayOrder = displayOrder;
    }
}
