using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using Shop.Domain.ValueObjects;

namespace Shop.Infrastructure.Persistence;

public class ShopDbContext : DbContext, IShopDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    public DbSet<BlogCategory> BlogCategories => Set<BlogCategory>();
    public DbSet<BlogComment> BlogComments => Set<BlogComment>();

    // Aura Leather Admin Entities
    public DbSet<FootwearProduct> FootwearProducts => Set<FootwearProduct>();
    public DbSet<FootwearVariant> FootwearVariants => Set<FootwearVariant>();
    public DbSet<AdminRole> AdminRoles => Set<AdminRole>();
    public DbSet<OrderTimelineEvent> OrderTimelineEvents => Set<OrderTimelineEvent>();
    public DbSet<ReturnRequest> ReturnRequests => Set<ReturnRequest>();
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
    public DbSet<CustomerNote> CustomerNotes => Set<CustomerNote>();
    public DbSet<CustomerSegment> CustomerSegments => Set<CustomerSegment>();
    public DbSet<LoyaltyRule> LoyaltyRules => Set<LoyaltyRule>();
    public DbSet<LoyaltyTier> LoyaltyTiers => Set<LoyaltyTier>();
    public DbSet<LoyaltyReward> LoyaltyRewards => Set<LoyaltyReward>();
    public DbSet<LoyaltyTransaction> LoyaltyTransactions => Set<LoyaltyTransaction>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<AbandonedCartRecord> AbandonedCartRecords => Set<AbandonedCartRecord>();
    public DbSet<CmsHomepageBlock> CmsHomepageBlocks => Set<CmsHomepageBlock>();
    public DbSet<SeoRedirect> SeoRedirects => Set<SeoRedirect>();
    public DbSet<SeoAuditIssue> SeoAuditIssues => Set<SeoAuditIssue>();
    public DbSet<SeoSetting> SeoSettings => Set<SeoSetting>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AdminNotification> AdminNotifications => Set<AdminNotification>();
    public DbSet<StoreSetting> StoreSettings => Set<StoreSetting>();

    public ShopDbContext(DbContextOptions<ShopDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure DateTimeOffset for SQLite in testing
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var properties = entityType.ClrType.GetProperties()
                    .Where(p => p.PropertyType == typeof(DateTimeOffset) || p.PropertyType == typeof(DateTimeOffset?));
                foreach (var property in properties)
                {
                    modelBuilder.Entity(entityType.ClrType).Property(property.Name).HasConversion(
                        new Microsoft.EntityFrameworkCore.Storage.ValueConversion.DateTimeOffsetToBinaryConverter());
                }
            }
        }

        // BlogCategory
        modelBuilder.Entity<BlogCategory>(b =>
        {
            b.ToTable("blog_categories");
            b.HasKey(c => c.Id);
            b.Property(c => c.Name).IsRequired().HasMaxLength(150);
            b.Property(c => c.Slug).IsRequired().HasMaxLength(150);
            b.HasIndex(c => c.Slug).IsUnique();
            b.Property(c => c.Description).HasMaxLength(500);
        });

        // BlogPost
        modelBuilder.Entity<BlogPost>(b =>
        {
            b.ToTable("blog_posts");
            b.HasKey(p => p.Id);
            b.Property(p => p.Title).IsRequired().HasMaxLength(300);
            b.Property(p => p.Slug).IsRequired().HasMaxLength(300);
            b.HasIndex(p => p.Slug).IsUnique();
            b.Property(p => p.Summary).IsRequired().HasMaxLength(1000);
            b.Property(p => p.Content).IsRequired();
            b.Property(p => p.AuthorName).IsRequired().HasMaxLength(150);
            b.Property(p => p.CoverImageUrl).HasMaxLength(500);
            b.Property(p => p.Tags).HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
            );

            b.HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasMany(p => p.Comments)
                .WithOne()
                .HasForeignKey(c => c.BlogPostId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // BlogComment
        modelBuilder.Entity<BlogComment>(b =>
        {
            b.ToTable("blog_comments");
            b.HasKey(c => c.Id);
            b.Property(c => c.UserName).IsRequired().HasMaxLength(150);
            b.Property(c => c.UserEmail).IsRequired().HasMaxLength(200);
            b.Property(c => c.Content).IsRequired().HasMaxLength(2000);
        });

        // User
        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("users");
            b.HasKey(u => u.Id);
            b.Property(u => u.Email).IsRequired().HasMaxLength(200);
            b.HasIndex(u => u.Email).IsUnique();
            b.Property(u => u.FullName).IsRequired().HasMaxLength(200);
            b.Property(u => u.PasswordHash).IsRequired().HasMaxLength(256);
            b.Property(u => u.Role).IsRequired().HasMaxLength(50);
            b.Property(u => u.PhoneNumber).HasMaxLength(50);

            b.HasMany(u => u.RefreshTokens)
                .WithOne()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(b =>
        {
            b.ToTable("refresh_tokens");
            b.HasKey(r => r.Id);
            b.Property(r => r.Token).IsRequired().HasMaxLength(256);
            b.HasIndex(r => r.Token);
        });

        // Category
        modelBuilder.Entity<Category>(b =>
        {
            b.ToTable("categories");
            b.HasKey(c => c.Id);
            b.Property(c => c.Name).IsRequired().HasMaxLength(150);
            b.Property(c => c.Slug).IsRequired().HasMaxLength(150);
            b.HasIndex(c => c.Slug).IsUnique();
            b.Property(c => c.Description).HasMaxLength(1000);
            b.Property(c => c.ImageUrl).HasMaxLength(500);

            b.HasOne(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Brand
        modelBuilder.Entity<Brand>(b =>
        {
            b.ToTable("brands");
            b.HasKey(b => b.Id);
            b.Property(b => b.Name).IsRequired().HasMaxLength(150);
            b.Property(b => b.Slug).IsRequired().HasMaxLength(150);
            b.HasIndex(b => b.Slug).IsUnique();
            b.Property(b => b.LogoUrl).HasMaxLength(500);
        });

        // Product
        modelBuilder.Entity<Product>(b =>
        {
            b.ToTable("products");
            b.HasKey(p => p.Id);
            b.Property(p => p.Name).IsRequired().HasMaxLength(250);
            b.Property(p => p.Slug).IsRequired().HasMaxLength(250);
            b.HasIndex(p => p.Slug).IsUnique();
            b.Property(p => p.Description).IsRequired();

            b.ComplexProperty(p => p.Sku, sku =>
            {
                sku.Property(s => s.Value).HasColumnName("sku").IsRequired().HasMaxLength(100);
            });

            b.ComplexProperty(p => p.Price, price =>
            {
                price.Property(m => m.Amount).HasColumnName("price_amount").HasPrecision(18, 2);
                price.Property(m => m.Currency).HasColumnName("price_currency").HasMaxLength(10);
            });

            b.OwnsOne(p => p.DiscountPrice, dp =>
            {
                dp.Property(m => m.Amount).HasColumnName("discount_price_amount").HasPrecision(18, 2);
                dp.Property(m => m.Currency).HasColumnName("discount_price_currency").HasMaxLength(10);
            });

            b.HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(p => p.Brand)
                .WithMany()
                .HasForeignKey(p => p.BrandId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasMany(p => p.Images)
                .WithOne()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProductImage>(b =>
        {
            b.ToTable("product_images");
            b.HasKey(i => i.Id);
            b.Property(i => i.ImageUrl).IsRequired().HasMaxLength(500);
        });

        // Cart
        modelBuilder.Entity<Cart>(b =>
        {
            b.ToTable("carts");
            b.HasKey(c => c.Id);
            b.HasIndex(c => c.UserId).IsUnique();

            b.HasMany(c => c.Items)
                .WithOne()
                .HasForeignKey(i => i.CartId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CartItem>(b =>
        {
            b.ToTable("cart_items");
            b.HasKey(i => i.Id);
            b.Property(i => i.ProductName).IsRequired().HasMaxLength(250);
            b.Property(i => i.Sku).IsRequired().HasMaxLength(100);
            b.Property(i => i.UnitPrice).HasPrecision(18, 2);
            b.Property(i => i.ImageUrl).HasMaxLength(500);
        });

        // Order
        modelBuilder.Entity<Order>(b =>
        {
            b.ToTable("orders");
            b.HasKey(o => o.Id);
            b.Property(o => o.OrderNumber).IsRequired().HasMaxLength(100);
            b.HasIndex(o => o.OrderNumber).IsUnique();
            b.Property(o => o.Status).HasConversion<int>();
            b.Property(o => o.PaymentStatus).HasConversion<int>();
            b.Property(o => o.TotalAmount).HasPrecision(18, 2);
            b.Property(o => o.DiscountAmount).HasPrecision(18, 2);
            b.Property(o => o.FinalAmount).HasPrecision(18, 2);
            b.Property(o => o.PaymentTransactionId).HasMaxLength(100);
            b.Property(o => o.CancellationReason).HasMaxLength(500);

            b.ComplexProperty(o => o.ShippingAddress, a =>
            {
                a.Property(x => x.Street).HasColumnName("shipping_street").HasMaxLength(250);
                a.Property(x => x.City).HasColumnName("shipping_city").HasMaxLength(100);
                a.Property(x => x.State).HasColumnName("shipping_state").HasMaxLength(100);
                a.Property(x => x.PostalCode).HasColumnName("shipping_postal_code").HasMaxLength(50);
                a.Property(x => x.Country).HasColumnName("shipping_country").HasMaxLength(100);
                a.Property(x => x.RecipientName).HasColumnName("shipping_recipient_name").HasMaxLength(150);
                a.Property(x => x.PhoneNumber).HasColumnName("shipping_phone").HasMaxLength(50);
            });

            b.HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasMany(o => o.Items)
                .WithOne()
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(b =>
        {
            b.ToTable("order_items");
            b.HasKey(i => i.Id);
            b.Property(i => i.ProductName).IsRequired().HasMaxLength(250);
            b.Property(i => i.Sku).IsRequired().HasMaxLength(100);
            b.Property(i => i.UnitPrice).HasPrecision(18, 2);
            b.Property(i => i.TotalPrice).HasPrecision(18, 2);
            b.Property(i => i.ImageUrl).HasMaxLength(500);
        });

        // PaymentTransaction
        modelBuilder.Entity<PaymentTransaction>(b =>
        {
            b.ToTable("payment_transactions");
            b.HasKey(p => p.Id);
            b.Property(p => p.GatewayName).IsRequired().HasMaxLength(100);
            b.Property(p => p.TransactionReference).IsRequired().HasMaxLength(100);
            b.HasIndex(p => p.TransactionReference).IsUnique();
            b.Property(p => p.Amount).HasPrecision(18, 2);
            b.Property(p => p.Status).HasConversion<int>();
            b.Property(p => p.ErrorMessage).HasMaxLength(500);
        });

        // FootwearProduct
        modelBuilder.Entity<FootwearProduct>(b =>
        {
            b.ToTable("footwear_products");
            b.HasKey(p => p.Id);
            b.Property(p => p.PersianName).IsRequired().HasMaxLength(250);
            b.Property(p => p.Name).IsRequired().HasMaxLength(250);
            b.Property(p => p.Slug).IsRequired().HasMaxLength(250);
            b.HasIndex(p => p.Slug).IsUnique();
            b.Property(p => p.Sku).IsRequired().HasMaxLength(100);
            b.HasIndex(p => p.Sku).IsUnique();
            b.Property(p => p.Category).HasMaxLength(100);
            b.Property(p => p.CategoryName).HasMaxLength(100);
            b.Property(p => p.Collection).HasMaxLength(150);
            b.Property(p => p.Gender).HasMaxLength(50);
            b.Property(p => p.BasePrice).HasPrecision(18, 2);
            b.Property(p => p.DiscountPrice).HasPrecision(18, 2);
            b.Property(p => p.CostPrice).HasPrecision(18, 2);

            b.OwnsOne(p => p.Specs, s =>
            {
                s.Property(x => x.Material).HasColumnName("spec_material").HasMaxLength(200);
                s.Property(x => x.LeatherType).HasColumnName("spec_leather_type").HasMaxLength(200);
                s.Property(x => x.Tannery).HasColumnName("spec_tannery").HasMaxLength(200);
                s.Property(x => x.SoleMaterial).HasColumnName("spec_sole_material").HasMaxLength(200);
                s.Property(x => x.Construction).HasColumnName("spec_construction").HasMaxLength(200);
                s.Property(x => x.Origin).HasColumnName("spec_origin").HasMaxLength(200);
            });

            b.OwnsOne(p => p.Seo, s =>
            {
                s.Property(x => x.Title).HasColumnName("seo_title").HasMaxLength(300);
                s.Property(x => x.MetaDescription).HasColumnName("seo_meta_desc").HasMaxLength(1000);
                s.Property(x => x.CanonicalUrl).HasColumnName("seo_canonical").HasMaxLength(500);
                s.Property(x => x.FocusKeyword).HasColumnName("seo_keyword").HasMaxLength(200);
            });

            b.Property(p => p.Images).HasConversion(
                v => string.Join(';', v),
                v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList());

            b.Property(p => p.CareInstructions).HasConversion(
                v => string.Join(';', v),
                v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList());

            b.HasMany(p => p.Variants)
                .WithOne()
                .HasForeignKey(v => v.FootwearProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // FootwearVariant
        modelBuilder.Entity<FootwearVariant>(b =>
        {
            b.ToTable("footwear_variants");
            b.HasKey(v => v.Id);
            b.Property(v => v.ColorName).HasMaxLength(100);
            b.Property(v => v.ColorHex).HasMaxLength(20);
            b.Property(v => v.Sku).IsRequired().HasMaxLength(100);
        });

        // AdminRole
        modelBuilder.Entity<AdminRole>(b =>
        {
            b.ToTable("admin_roles");
            b.HasKey(r => r.Id);
            b.Property(r => r.Name).IsRequired().HasMaxLength(100);
            b.HasIndex(r => r.Name).IsUnique();
            b.Property(r => r.NameFa).IsRequired().HasMaxLength(100);
            b.Property(r => r.Permissions).HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());
        });

        // OrderTimelineEvent
        modelBuilder.Entity<OrderTimelineEvent>(b =>
        {
            b.ToTable("order_timeline_events");
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.OrderId);
        });

        // ReturnRequest
        modelBuilder.Entity<ReturnRequest>(b =>
        {
            b.ToTable("return_requests");
            b.HasKey(r => r.Id);
            b.Property(r => r.RefundAmount).HasPrecision(18, 2);
        });

        // CustomerProfile
        modelBuilder.Entity<CustomerProfile>(b =>
        {
            b.ToTable("customer_profiles");
            b.HasKey(c => c.Id);
            b.HasIndex(c => c.UserId).IsUnique();
            b.Property(c => c.TotalSpent).HasPrecision(18, 2);
            b.Property(c => c.WalletBalance).HasPrecision(18, 2);
        });

        // CustomerNote
        modelBuilder.Entity<CustomerNote>(b =>
        {
            b.ToTable("customer_notes");
            b.HasKey(n => n.Id);
            b.HasIndex(n => n.CustomerId);
        });

        // CustomerSegment
        modelBuilder.Entity<CustomerSegment>(b =>
        {
            b.ToTable("customer_segments");
            b.HasKey(s => s.Id);
        });

        // LoyaltyRule
        modelBuilder.Entity<LoyaltyRule>(b =>
        {
            b.ToTable("loyalty_rules");
            b.HasKey(r => r.Id);
        });

        // LoyaltyTier
        modelBuilder.Entity<LoyaltyTier>(b =>
        {
            b.ToTable("loyalty_tiers");
            b.HasKey(t => t.Id);
            b.Property(t => t.MinSpend).HasPrecision(18, 2);
            b.Property(t => t.Perks).HasConversion(
                v => string.Join(';', v),
                v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList());
        });

        // LoyaltyReward
        modelBuilder.Entity<LoyaltyReward>(b =>
        {
            b.ToTable("loyalty_rewards");
            b.HasKey(r => r.Id);
            b.Property(r => r.DiscountValue).HasPrecision(18, 2);
        });

        // LoyaltyTransaction
        modelBuilder.Entity<LoyaltyTransaction>(b =>
        {
            b.ToTable("loyalty_transactions");
            b.HasKey(t => t.Id);
        });

        // Coupon
        modelBuilder.Entity<Coupon>(b =>
        {
            b.ToTable("coupons");
            b.HasKey(c => c.Id);
            b.Property(c => c.Code).IsRequired().HasMaxLength(100);
            b.HasIndex(c => c.Code).IsUnique();
            b.Property(c => c.Value).HasPrecision(18, 2);
            b.Property(c => c.MaxDiscount).HasPrecision(18, 2);
            b.Property(c => c.MinCartSpend).HasPrecision(18, 2);
            b.Property(c => c.TargetTiers).HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());
        });

        // Campaign
        modelBuilder.Entity<Campaign>(b =>
        {
            b.ToTable("campaigns");
            b.HasKey(c => c.Id);
            b.Property(c => c.Revenue).HasPrecision(18, 2);
            b.Property(c => c.SpendCost).HasPrecision(18, 2);
        });

        // AbandonedCartRecord
        modelBuilder.Entity<AbandonedCartRecord>(b =>
        {
            b.ToTable("abandoned_cart_records");
            b.HasKey(a => a.Id);
            b.Property(a => a.TotalAmount).HasPrecision(18, 2);
        });

        // CmsHomepageBlock
        modelBuilder.Entity<CmsHomepageBlock>(b =>
        {
            b.ToTable("cms_homepage_blocks");
            b.HasKey(b => b.Id);
        });

        // SeoRedirect
        modelBuilder.Entity<SeoRedirect>(b =>
        {
            b.ToTable("seo_redirects");
            b.HasKey(r => r.Id);
        });

        // SeoAuditIssue
        modelBuilder.Entity<SeoAuditIssue>(b =>
        {
            b.ToTable("seo_audit_issues");
            b.HasKey(i => i.Id);
        });

        // SeoSetting
        modelBuilder.Entity<SeoSetting>(b =>
        {
            b.ToTable("seo_settings");
            b.HasKey(s => s.Id);
        });

        // AuditLog
        modelBuilder.Entity<AuditLog>(b =>
        {
            b.ToTable("audit_logs");
            b.HasKey(a => a.Id);
        });

        // AdminNotification
        modelBuilder.Entity<AdminNotification>(b =>
        {
            b.ToTable("admin_notifications");
            b.HasKey(n => n.Id);
        });

        // StoreSetting
        modelBuilder.Entity<StoreSetting>(b =>
        {
            b.ToTable("store_settings");
            b.HasKey(s => s.Id);
            b.Property(s => s.FreeShippingThreshold).HasPrecision(18, 2);
            b.Property(s => s.DefaultShippingFee).HasPrecision(18, 2);
            b.Property(s => s.TaxPercent).HasPrecision(18, 2);
        });

        // All domain entities use client-generated GUID IDs (Guid.NewGuid()).
        // Explicitly set ValueGenerated.Never on Guid Id properties so EF Core doesn't assume
        // a pre-set ID indicates an existing database record when child entities are added to tracked collections.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var idProp = entityType.FindProperty("Id");
            if (idProp != null && idProp.ClrType == typeof(Guid))
            {
                idProp.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
            }
        }
    }
}
