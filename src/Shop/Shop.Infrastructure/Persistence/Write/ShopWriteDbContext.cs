using Microsoft.EntityFrameworkCore;
using Shop.Domain.Entities;
using Shop.Domain.ValueObjects;

namespace Shop.Infrastructure.Persistence.Write;

public class ShopWriteDbContext : DbContext
{
    public ShopWriteDbContext(DbContextOptions<ShopWriteDbContext> options) : base(options) { }

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Blog
        modelBuilder.Entity<BlogCategory>(b =>
        {
            b.ToTable("blog_categories");
            b.HasKey(c => c.Id);
            b.Property(c => c.Name).IsRequired().HasMaxLength(150);
            b.Property(c => c.Slug).IsRequired().HasMaxLength(150);
            b.HasIndex(c => c.Slug).IsUnique();
            b.Property(c => c.Description).HasMaxLength(1000);
        });

        modelBuilder.Entity<BlogPost>(b =>
        {
            b.ToTable("blog_posts");
            b.HasKey(p => p.Id);
            b.Property(p => p.Title).IsRequired().HasMaxLength(300);
            b.Property(p => p.Slug).IsRequired().HasMaxLength(300);
            b.HasIndex(p => p.Slug).IsUnique();
            b.Property(p => p.Summary).IsRequired().HasMaxLength(1000);
            b.Property(p => p.Content).IsRequired();
            b.Property(p => p.CoverImageUrl).HasMaxLength(500);
            b.Property(p => p.AuthorName).IsRequired().HasMaxLength(150);

            b.HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasMany(p => p.Comments)
                .WithOne()
                .HasForeignKey(c => c.BlogPostId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BlogComment>(b =>
        {
            b.ToTable("blog_comments");
            b.HasKey(c => c.Id);
            b.Property(c => c.UserName).IsRequired().HasMaxLength(150);
            b.Property(c => c.UserEmail).IsRequired().HasMaxLength(250);
            b.Property(c => c.Content).IsRequired().HasMaxLength(2000);
        });

        // User
        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("users");
            b.HasKey(u => u.Id);
            b.Property(u => u.Email).IsRequired().HasMaxLength(250);
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

            b.OwnsOne(o => o.ShippingAddress, a =>
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
    }
}
