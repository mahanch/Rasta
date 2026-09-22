using Shop.Domain.Common;

namespace Shop.Domain.Entities;

public class SeoRedirect : Entity<Guid>
{
    public string FromUrl { get; private set; } = string.Empty;
    public string ToUrl { get; private set; } = string.Empty;
    public int Type { get; private set; } = 301; // 301 or 302
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private SeoRedirect() { }

    public SeoRedirect(string fromUrl, string toUrl, int type = 301, Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        FromUrl = fromUrl.Trim();
        ToUrl = toUrl.Trim();
        Type = type;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}

public class SeoAuditIssue : Entity<Guid>
{
    public string EntityType { get; private set; } = "product";
    public string EntityName { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public string IssueType { get; private set; } = "missing_alt"; // missing_title, missing_description, missing_alt, broken_link
    public string Severity { get; private set; } = "medium"; // low, medium, high
    public string Recommendation { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private SeoAuditIssue() { }

    public SeoAuditIssue(
        string entityType,
        string entityName,
        string url,
        string issueType,
        string severity,
        string recommendation,
        Guid? id = null) : base(id ?? Guid.NewGuid())
    {
        EntityType = entityType;
        EntityName = entityName;
        Url = url;
        IssueType = issueType;
        Severity = severity;
        Recommendation = recommendation;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}

public class SeoSetting : Entity<Guid>
{
    public string RobotsContent { get; set; } = "User-agent: *\nAllow: /\nDisallow: /api/\nDisallow: /admin/\nSitemap: https://aura-leather.ir/sitemap.xml";
    public string SitemapUrl { get; set; } = "https://aura-leather.ir/sitemap.xml";
    public int HealthScore { get; set; } = 91;

    public SeoSetting() : base(Guid.NewGuid()) { }
}
