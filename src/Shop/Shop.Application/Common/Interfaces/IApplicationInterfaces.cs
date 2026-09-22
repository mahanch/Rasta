using Microsoft.EntityFrameworkCore;
using Shop.Application.DTOs;
using Shop.Domain.Entities;
using Shop.Domain.ValueObjects;

namespace Shop.Application.Common.Interfaces;

public interface ILicenseClientService
{
    Task<LicenseState> GetOrRefreshLicenseStateAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
    Task<bool> CanProcessOrderAsync(CancellationToken cancellationToken = default);
    Task<bool> RecordOrderUsageAsync(CancellationToken cancellationToken = default);
    LicenseState GetCurrentLicenseState();
}

public interface IJwtTokenService
{
    AuthResponseDto GenerateTokens(Guid userId, string email, string fullName, string role);
    AdminLoginResponse GenerateAdminTokens(Guid userId, string email, string fullName, string role, string roleNameFa, List<string> permissions);
}

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

public interface IAuditLogService
{
    Task LogAsync(string adminId, string adminName, string adminRole, string action, string entity, string beforeValue, string afterValue, string ipAddress, CancellationToken ct = default);
}

public interface IShopDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Category> Categories { get; }
    DbSet<Brand> Brands { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductImage> ProductImages { get; }
    DbSet<Cart> Carts { get; }
    DbSet<CartItem> CartItems { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<PaymentTransaction> PaymentTransactions { get; }
    DbSet<BlogPost> BlogPosts { get; }
    DbSet<BlogCategory> BlogCategories { get; }
    DbSet<BlogComment> BlogComments { get; }

    // Aura Leather Admin Entities
    DbSet<FootwearProduct> FootwearProducts { get; }
    DbSet<FootwearVariant> FootwearVariants { get; }
    DbSet<AdminRole> AdminRoles { get; }
    DbSet<OrderTimelineEvent> OrderTimelineEvents { get; }
    DbSet<ReturnRequest> ReturnRequests { get; }
    DbSet<CustomerProfile> CustomerProfiles { get; }
    DbSet<CustomerNote> CustomerNotes { get; }
    DbSet<CustomerSegment> CustomerSegments { get; }
    DbSet<LoyaltyRule> LoyaltyRules { get; }
    DbSet<LoyaltyTier> LoyaltyTiers { get; }
    DbSet<LoyaltyReward> LoyaltyRewards { get; }
    DbSet<LoyaltyTransaction> LoyaltyTransactions { get; }
    DbSet<Coupon> Coupons { get; }
    DbSet<Campaign> Campaigns { get; }
    DbSet<AbandonedCartRecord> AbandonedCartRecords { get; }
    DbSet<CmsHomepageBlock> CmsHomepageBlocks { get; }
    DbSet<SeoRedirect> SeoRedirects { get; }
    DbSet<SeoAuditIssue> SeoAuditIssues { get; }
    DbSet<SeoSetting> SeoSettings { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<AdminNotification> AdminNotifications { get; }
    DbSet<StoreSetting> StoreSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
