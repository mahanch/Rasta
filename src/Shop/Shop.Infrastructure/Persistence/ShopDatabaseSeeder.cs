using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Shop.Application.Common.Interfaces;
using Shop.Application.ReadModels;
using Shop.Domain.Entities;
using Shop.Domain.ValueObjects;
using Shop.Infrastructure.Persistence.Write;

namespace Shop.Infrastructure.Persistence;

public class ShopDatabaseSeeder
{
    private readonly ShopWriteDbContext _db;
    private readonly IMongoReadDbContext _mongo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<ShopDatabaseSeeder> _logger;

    public ShopDatabaseSeeder(
        ShopWriteDbContext db,
        IMongoReadDbContext mongo,
        IPasswordHasher passwordHasher,
        ILogger<ShopDatabaseSeeder> logger)
    {
        _db = db;
        _mongo = mongo;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await _db.Database.EnsureCreatedAsync(cancellationToken);

        // 1. Seed Admin & Demo Customer
        if (!await _db.Users.AnyAsync(cancellationToken))
        {
            var admin = new User("admin@shop.local", "Super Admin", _passwordHasher.HashPassword("Admin@123456"), "Admin", "+989120000000");
            var customer = new User("customer@shop.local", "John Doe", _passwordHasher.HashPassword("Customer@123456"), "Customer", "+989121111111");

            _db.Users.AddRange(admin, customer);
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded default Admin and Customer users.");
        }

        // 2. Seed Categories
        if (!await _db.Categories.AnyAsync(cancellationToken))
        {
            var catLaptops = new Category("Laptops & Computers", "laptops", "High performance laptops and desktops", "https://images.unsplash.com/photo-1496181133206-80ce9b88a853");
            var catPhones = new Category("Smartphones", "smartphones", "Latest flagship smartphones", "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9");
            var catAccessories = new Category("Accessories", "accessories", "Audio, chargers, and gadgets", "https://images.unsplash.com/photo-1505740420928-5e560c06d30e");

            _db.Categories.AddRange(catLaptops, catPhones, catAccessories);
            await _db.SaveChangesAsync(cancellationToken);

            // Sync to Mongo
            try
            {
                var catColl = _mongo.GetCollection<CategoryReadModel>("categories_view");
                foreach (var cat in new[] { catLaptops, catPhones, catAccessories })
                {
                    await catColl.ReplaceOneAsync(
                        c => c.Id == cat.Id,
                        new CategoryReadModel
                        {
                            Id = cat.Id,
                            Name = cat.Name,
                            Slug = cat.Slug,
                            Description = cat.Description,
                            ImageUrl = cat.ImageUrl,
                            IsActive = cat.IsActive,
                            CreatedAt = cat.CreatedAt
                        },
                        new ReplaceOptions { IsUpsert = true },
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not seed categories to MongoDB during startup.");
            }

            _logger.LogInformation("Seeded Categories.");
        }

        // 3. Seed Products
        if (!await _db.Products.AnyAsync(cancellationToken))
        {
            var catLaptops = await _db.Categories.FirstAsync(c => c.Slug == "laptops", cancellationToken);
            var catPhones = await _db.Categories.FirstAsync(c => c.Slug == "smartphones", cancellationToken);
            var catAccessories = await _db.Categories.FirstAsync(c => c.Slug == "accessories", cancellationToken);

            var p1 = new Product(
                "MacBook Pro 16 M3 Max",
                "macbook-pro-16-m3-max",
                "Apple M3 Max chip with 16-core CPU and 40-core GPU, 48GB Unified Memory, 1TB SSD Storage.",
                new Sku("MBP-16-M3MAX"),
                new Money(3499.00m),
                new Money(3299.00m),
                25,
                catLaptops.Id,
                catLaptops.Name,
                imageUrls: ["https://images.unsplash.com/photo-1517336714731-489689fd1ca8"]
            );

            var p2 = new Product(
                "ThinkPad X1 Carbon Gen 12",
                "thinkpad-x1-carbon-gen-12",
                "Intel Core Ultra 7 155H, 32GB LPDDR5x, 1TB PCIe NVMe SSD, 14.0 2.8K OLED Display.",
                new Sku("TP-X1C-G12"),
                new Money(1899.00m),
                null,
                15,
                catLaptops.Id,
                catLaptops.Name,
                imageUrls: ["https://images.unsplash.com/photo-1588872657578-7efd1f1555ed"]
            );

            var p3 = new Product(
                "iPhone 16 Pro Max 256GB",
                "iphone-16-pro-max-256gb",
                "Titanium design, A18 Pro chip, 48MP Fusion camera system with 5x Telephoto.",
                new Sku("IPHONE-16-PM"),
                new Money(1199.00m),
                null,
                40,
                catPhones.Id,
                catPhones.Name,
                imageUrls: ["https://images.unsplash.com/photo-1592750475338-74b7b21085ab"]
            );

            var p4 = new Product(
                "Sony WH-1000XM5 Wireless Headphones",
                "sony-wh-1000xm5",
                "Industry-leading noise canceling with two processors and 8 microphones, 30h battery.",
                new Sku("SONY-WH1000XM5"),
                new Money(399.00m),
                new Money(349.00m),
                50,
                catAccessories.Id,
                catAccessories.Name,
                imageUrls: ["https://images.unsplash.com/photo-1505740420928-5e560c06d30e"]
            );

            _db.Products.AddRange(p1, p2, p3, p4);
            await _db.SaveChangesAsync(cancellationToken);

            // Sync to Mongo
            try
            {
                var prodColl = _mongo.GetCollection<ProductReadModel>("products_view");
                foreach (var p in new[] { p1, p2, p3, p4 })
                {
                    await prodColl.ReplaceOneAsync(
                        x => x.Id == p.Id,
                        new ProductReadModel
                        {
                            Id = p.Id,
                            Name = p.Name,
                            Slug = p.Slug,
                            Description = p.Description,
                            Sku = p.Sku.Value,
                            Price = p.Price.Amount,
                            DiscountPrice = p.DiscountPrice?.Amount,
                            StockQuantity = p.StockQuantity,
                            IsActive = p.IsActive,
                            CategoryId = p.CategoryId,
                            CategoryName = p.Category.Name,
                            ImageUrls = p.Images.Select(i => i.ImageUrl).ToList(),
                            CreatedAt = p.CreatedAt
                        },
                        new ReplaceOptions { IsUpsert = true },
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not seed products to MongoDB during startup.");
            }

            _logger.LogInformation("Seeded Products.");
        }

        // 4. Seed Blog Categories & Posts
        if (!await _db.BlogCategories.AnyAsync(cancellationToken))
        {
            var techCat = new BlogCategory("Tech & Gadgets", "tech-gadgets", "Latest technological breakthroughs and gadget reviews");
            var guideCat = new BlogCategory("Buying Guides", "buying-guides", "Expert advice for buying the best electronics");

            _db.BlogCategories.AddRange(techCat, guideCat);
            await _db.SaveChangesAsync(cancellationToken);

            var admin = await _db.Users.FirstAsync(u => u.Role == "Admin", cancellationToken);

            var post1 = BlogPost.Create(
                "Top Tech Trends Shaping E-Commerce in 2026",
                "top-tech-trends-shaping-ecommerce-2026",
                "Explore how AI agents, modern distributed architectures, and instant checkout flows are transforming digital retail.",
                "The world of e-commerce has advanced rapidly. In this article, we dive into how microservices, event-driven architectures with RabbitMQ and CQRS separation provide resilient digital stores.",
                admin.Id,
                admin.FullName,
                techCat.Id,
                techCat.Name,
                "https://images.unsplash.com/photo-1451187580459-43490279c0fa",
                ["tech", "ecommerce", "architecture"],
                publishImmediately: true
            );

            post1.AddComment(null, "Sara", "sara@example.com", "Fascinating read! Loved the insights on CQRS and modern checkout pipelines.", autoApprove: true);

            var post2 = BlogPost.Create(
                "Ultimate Laptop Buying Guide for Developers & Creators",
                "ultimate-laptop-buying-guide-developers",
                "Everything you need to know before choosing your next development workstation or creative powerhouse.",
                "Whether you are compiling large .NET solutions or rendering 4K video, having sufficient RAM and multicore processor capabilities is key.",
                admin.Id,
                admin.FullName,
                guideCat.Id,
                guideCat.Name,
                "https://images.unsplash.com/photo-1496181133206-80ce9b88a853",
                ["laptops", "guide", "hardware"],
                publishImmediately: true
            );

            _db.BlogPosts.AddRange(post1, post2);
            await _db.SaveChangesAsync(cancellationToken);

            // Sync to Mongo
            try
            {
                var catColl = _mongo.GetCollection<BlogCategoryReadModel>("blog_categories_view");
                foreach (var cat in new[] { techCat, guideCat })
                {
                    await catColl.ReplaceOneAsync(
                        c => c.Id == cat.Id,
                        new BlogCategoryReadModel { Id = cat.Id, Name = cat.Name, Slug = cat.Slug, Description = cat.Description, CreatedAt = cat.CreatedAt },
                        new ReplaceOptions { IsUpsert = true },
                        cancellationToken);
                }

                var postColl = _mongo.GetCollection<BlogPostReadModel>("blog_posts_view");
                foreach (var post in new[] { post1, post2 })
                {
                    await postColl.ReplaceOneAsync(
                        p => p.Id == post.Id,
                        new BlogPostReadModel
                        {
                            Id = post.Id,
                            Title = post.Title,
                            Slug = post.Slug,
                            Summary = post.Summary,
                            Content = post.Content,
                            CoverImageUrl = post.CoverImageUrl,
                            AuthorId = post.AuthorId,
                            AuthorName = post.AuthorName,
                            CategoryId = post.CategoryId,
                            CategoryName = post.Category?.Name,
                            Tags = post.Tags,
                            ReadingTimeMinutes = post.ReadingTimeMinutes,
                            IsPublished = post.IsPublished,
                            PublishedAt = post.PublishedAt,
                            CreatedAt = post.CreatedAt,
                            Comments = post.Comments.Select(c => new BlogCommentReadModel
                            {
                                Id = c.Id,
                                UserId = c.UserId,
                                UserName = c.UserName,
                                Content = c.Content,
                                IsApproved = c.IsApproved,
                                CreatedAt = c.CreatedAt
                            }).ToList()
                        },
                        new ReplaceOptions { IsUpsert = true },
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not seed blog posts to MongoDB during startup.");
            }

            _logger.LogInformation("Seeded Blog categories and posts.");
        }
    }
}
