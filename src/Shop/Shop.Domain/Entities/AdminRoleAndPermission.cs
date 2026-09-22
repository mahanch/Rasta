using Shop.Domain.Common;

namespace Shop.Domain.Entities;

public class AdminRole : Entity<Guid>
{
    public string Name { get; private set; } = string.Empty; // e.g. "super_admin", "manager", "content_manager", "marketing_manager", "support"
    public string NameFa { get; private set; } = string.Empty; // e.g. "مدیر ارشد پلتفرم"
    public string Description { get; private set; } = string.Empty;
    public List<string> Permissions { get; private set; } = [];
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; private set; }

    private AdminRole() { }

    public AdminRole(string name, string nameFa, string description, List<string> permissions, Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        Name = name.Trim().ToLowerInvariant();
        NameFa = nameFa.Trim();
        Description = description;
        Permissions = permissions ?? [];
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdatePermissions(List<string> permissions)
    {
        Permissions = permissions ?? [];
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool HasPermission(string permission)
    {
        if (Permissions.Contains("*")) return true;
        if (Permissions.Contains(permission)) return true;

        var dotIndex = permission.IndexOf('.');
        if (dotIndex > 0)
        {
            var wildcard = permission[..(dotIndex + 1)] + "*";
            if (Permissions.Contains(wildcard)) return true;
        }

        return false;
    }
}
