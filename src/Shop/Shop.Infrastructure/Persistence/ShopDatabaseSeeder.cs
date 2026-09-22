using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using Shop.Domain.ValueObjects;

namespace Shop.Infrastructure.Persistence;

public class ShopDatabaseSeeder
{
    private readonly ShopDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<ShopDatabaseSeeder> _logger;

    public ShopDatabaseSeeder(
        ShopDbContext db,
        IPasswordHasher passwordHasher,
        ILogger<ShopDatabaseSeeder> logger)
    {
        _db = db;
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
                "Explore how modern distributed architectures and CQRS pipelines provide resilient digital stores.",
                "The world of e-commerce has advanced rapidly. In this article, we dive into how clean CQRS separation and relational persistence provide resilient digital stores.",
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
            _logger.LogInformation("Seeded Blog categories and posts.");
        }

        // 5. Seed Aura Leather Admin Data
        await SeedAuraLeatherAsync(cancellationToken);
    }

    private async Task SeedAuraLeatherAsync(CancellationToken cancellationToken)
    {
        // A. Seed Super Admin and Staff
        if (!await _db.Users.AnyAsync(u => u.Email == "r.tehrani@aura-leather.ir", cancellationToken))
        {
            var superAdmin = new User(
                "r.tehrani@aura-leather.ir",
                "رضا تهرانی",
                _passwordHasher.HashPassword("StrongPassword!123"),
                "super_admin",
                "09121234567"
            );

            var manager = new User(
                "k.yazdani@aura-leather.ir",
                "کامران یزدانی",
                _passwordHasher.HashPassword("Manager@123"),
                "manager",
                "09129876543"
            );

            _db.Users.AddRange(superAdmin, manager);
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded Aura Leather Admin Users.");
        }

        // B. Seed Admin Roles & Permissions
        if (!await _db.AdminRoles.AnyAsync(cancellationToken))
        {
            var roles = new List<AdminRole>
            {
                new("super_admin", "مدیر ارشد پلتفرم", "دسترسی کامل به تمامی بخش‌های سیستم", ["*"]),
                new("manager", "مدیر فروشگاه", "مدیریت سفارشات، محصولات، انبارداری و مشتریان",
                    ["products.*", "orders.*", "inventory.*", "returns.*", "customers.*", "dashboard.read"]),
                new("content_manager", "مدیر محتوا و سئو", "مدیریت مقالات، بلوک‌های صفحه نخست و تنظیمات سئو",
                    ["blog.*", "cms.*", "seo.*"]),
                new("marketing_manager", "مدیر بازاریابی", "مدیریت کوپن‌ها، کمپین‌ها، سبدهای رهاشده و باشگاه مشتریان",
                    ["marketing.*", "coupons.*", "campaigns.*", "loyalty.*", "abandoned-carts.*"]),
                new("support", "کارشناس پشتیبانی", "پاسخگویی به مشتریان و بررسی وضعیت سفارشات و مرجوعی‌ها",
                    ["orders.read", "orders.update", "returns.*", "customers.read"])
            };

            _db.AdminRoles.AddRange(roles);
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded Admin Roles.");
        }

        // C. Seed Footwear Products
        if (!await _db.FootwearProducts.AnyAsync(cancellationToken))
        {
            var p1 = new FootwearProduct(
                persianName: "کفش آکسفورد کلاسیک نوک کلاهدار",
                name: "Classic Oxford Cap-Toe",
                slug: "classic-oxford-cap-toe",
                sku: "AUR-OXF-001",
                category: "formal",
                categoryName: "کفش رسمی",
                collection: "کفش‌های دست‌دوز شاهکار",
                gender: "men",
                basePrice: 6850000,
                discountPrice: 6250000,
                costPrice: 3400000,
                specs: new FootwearSpecs
                {
                    Material = "چرم ۱۰۰٪ طبیعی گاوی فول گرین",
                    LeatherType = "چرم تمام‌دانه دباغی گیاهی",
                    Tannery = "کارگاه دباغی خسروی، تبریز",
                    SoleMaterial = "زیره چرم طبیعی فشرده گاومیش با پاشنه Dainite",
                    Construction = "دوخت سنتی گودیر ولتد (Goodyear Welted)",
                    Origin = "دست‌دوز تبریز، ایران"
                },
                seo: new FootwearSeo
                {
                    Title = "خرید کفش آکسفورد کلاسیک مردانه چرم طبیعی دست‌دوز تبریز | چرم اورا",
                    MetaDescription = "خرید اینترنتی کفش آکسفورد دست‌دوز چرم تمام دانه تبریز با ضمانت تعویض سایز.",
                    CanonicalUrl = "https://aura-leather.ir/product/classic-oxford-cap-toe",
                    FocusKeyword = "کفش آکسفورد کلاسیک چرم"
                },
                variants:
                [
                    new FootwearVariant(39, "قهوه‌ای تیره", "#3B2314", "AUR-OXF-001-39", 2, 3),
                    new FootwearVariant(40, "قهوه‌ای تیره", "#3B2314", "AUR-OXF-001-40", 5, 3),
                    new FootwearVariant(41, "قهوه‌ای تیره", "#3B2314", "AUR-OXF-001-41", 4, 3),
                    new FootwearVariant(42, "قهوه‌ای تیره", "#3B2314", "AUR-OXF-001-42", 8, 3),
                    new FootwearVariant(43, "قهوه‌ای تیره", "#3B2314", "AUR-OXF-001-43", 3, 3),
                    new FootwearVariant(44, "قهوه‌ای تیره", "#3B2314", "AUR-OXF-001-44", 2, 3),
                    new FootwearVariant(45, "قهوه‌ای تیره", "#3B2314", "AUR-OXF-001-45", 0, 3)
                ],
                images: ["https://cdn.aura-leather.ir/products/oxford-1.jpg"],
                shortDescription: "کفش آکسفورد تمام‌چرم دست‌دوز با پرداخت موم طبیعی عسل.",
                fullDescription: "توضیحات کامل درباره تاریخچه و دوخت محصول...",
                careInstructions: ["استفاده همیشگی از قالب چوبی سدر", "پولیش منظم با واکس ارگانیک"],
                status: "published"
            );

            var p2 = new FootwearProduct(
                persianName: "کفش دابل مانک استرپ کنیاکی",
                name: "Double Monk Strap Cognac",
                slug: "monk-strap-double-cognac",
                sku: "AUR-MNK-002",
                category: "formal",
                categoryName: "کفش رسمی",
                collection: "کفش‌های دست‌دوز شاهکار",
                gender: "men",
                basePrice: 7200000,
                discountPrice: 6800000,
                costPrice: 3600000,
                specs: new FootwearSpecs
                {
                    Material = "چرم طبیعی گاوی صادراتی",
                    LeatherType = "چرم آنیلین با فینیش طبیعی",
                    Tannery = "کارگاه تبریز",
                    SoleMaterial = "زیره دو لایه چرمی فشرده",
                    Construction = "دوخت بلیک راپید",
                    Origin = "دست‌دوز تبریز، ایران"
                },
                seo: new FootwearSeo
                {
                    Title = "خرید کفش دابل مانک استرپ مردانه چرم کنیاکی | چرم اورا",
                    MetaDescription = "کفش سگک‌دار دابل مانک استرپ چرم اصل دست‌دوز با قالب استاندارد ارگونومیک.",
                    CanonicalUrl = "https://aura-leather.ir/product/monk-strap-double-cognac",
                    FocusKeyword = "کفش دابل مانک استرپ"
                },
                variants:
                [
                    new FootwearVariant(39, "کنیاکی", "#8B4513", "AUR-MNK-002-39", 1, 3),
                    new FootwearVariant(40, "کنیاکی", "#8B4513", "AUR-MNK-002-40", 3, 3),
                    new FootwearVariant(41, "کنیاکی", "#8B4513", "AUR-MNK-002-41", 5, 3),
                    new FootwearVariant(42, "کنیاکی", "#8B4513", "AUR-MNK-002-42", 6, 3),
                    new FootwearVariant(43, "کنیاکی", "#8B4513", "AUR-MNK-002-43", 4, 3),
                    new FootwearVariant(44, "کنیاکی", "#8B4513", "AUR-MNK-002-44", 1, 3),
                    new FootwearVariant(45, "کنیاکی", "#8B4513", "AUR-MNK-002-45", 0, 3)
                ],
                images: ["https://cdn.aura-leather.ir/products/monk-1.jpg"],
                shortDescription: "کفش سگک‌دار دابل مانک استرپ با استایل متمایز و شیک.",
                fullDescription: "کفش دابل مانک اورا از مرغوب‌ترین پوست طبیعی گاو تهیه شده است...",
                careInstructions: ["استفاده از پاشنه‌کش برای حفظ فرم پشت کفش", "واکس گیاهی بدون سیلیکون"],
                status: "published"
            );

            var p3 = new FootwearProduct(
                persianName: "بوت چلسی کلاسیک چرم قهوه‌ای سوخته",
                name: "Chelsea Boot Heritage",
                slug: "chelsea-boot-heritage",
                sku: "AUR-BOT-003",
                category: "boots",
                categoryName: "بوت و نیم‌بوت",
                collection: "کالکشن زمستانه اورا",
                gender: "men",
                basePrice: 8500000,
                discountPrice: 7900000,
                costPrice: 4200000,
                specs: new FootwearSpecs
                {
                    Material = "چرم چرب پولاپ طبیعی",
                    LeatherType = "روغنی ضد رطوبت",
                    Tannery = "تبریز",
                    SoleMaterial = "زیره رابر دندانه‌دار مقاوم در برابر سایش",
                    Construction = "استورم ولتد (Storm Welted)",
                    Origin = "دست‌دوز تبریز، ایران"
                },
                seo: new FootwearSeo
                {
                    Title = "خرید نیم‌بوت چلسی مردانه چرم طبیعی دست‌دوز | چرم اورا",
                    MetaDescription = "نیم بوت چلسی تمام چرم مردانه با کش جانبی ایتالیایی و زیره عاج‌دار زمستانه.",
                    CanonicalUrl = "https://aura-leather.ir/product/chelsea-boot-heritage",
                    FocusKeyword = "بوت چلسی مردانه چرم"
                },
                variants:
                [
                    new FootwearVariant(39, "قهوه‌ای سوخته", "#2B1B17", "AUR-BOT-003-39", 3, 3),
                    new FootwearVariant(40, "قهوه‌ای سوخته", "#2B1B17", "AUR-BOT-003-40", 4, 3),
                    new FootwearVariant(41, "قهوه‌ای سوخته", "#2B1B17", "AUR-BOT-003-41", 6, 3),
                    new FootwearVariant(42, "قهوه‌ای سوخته", "#2B1B17", "AUR-BOT-003-42", 7, 3),
                    new FootwearVariant(43, "قهوه‌ای سوخته", "#2B1B17", "AUR-BOT-003-43", 5, 3),
                    new FootwearVariant(44, "قهوه‌ای سوخته", "#2B1B17", "AUR-BOT-003-44", 3, 3),
                    new FootwearVariant(45, "قهوه‌ای سوخته", "#2B1B17", "AUR-BOT-003-45", 2, 3)
                ],
                images: ["https://cdn.aura-leather.ir/products/chelsea-1.jpg"],
                shortDescription: "بوت چلسی دست‌دوز با کش بادوام و زیره گریپ‌دار زمستانه.",
                fullDescription: "این بوت ترکیبی از استایل مدرن و دوام کلاسیک کارگاهی است...",
                careInstructions: ["تمیزکاری با برس اسب بعد از هر استفاده در هوای بارانی"],
                status: "published"
            );

            _db.FootwearProducts.AddRange(p1, p2, p3);
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded Footwear Products & Variants.");
        }

        // D. Seed Loyalty Club Rules & Tiers
        if (!await _db.LoyaltyRules.AnyAsync(cancellationToken))
        {
            _db.LoyaltyRules.Add(new LoyaltyRule
            {
                PurchaseRatio = 10,
                RegisterPoints = 100,
                ReviewPoints = 50,
                ReferralPoints = 200,
                PointExpiryDays = 365
            });

            _db.LoyaltyTiers.AddRange(
                new LoyaltyTier("bronze", "برنزی", 0, 0, 0, ["ارسال سریع سفارشات"]),
                new LoyaltyTier("silver", "نقره‌ای", 10000000, 500, 5, ["۵٪ تخفیف روی تمام محصولات", "ارسال رایگان سفارشات"]),
                new LoyaltyTier("gold", "طلایی", 25000000, 1200, 10, ["۱۰٪ تخفیف روی تمام محصولات", "بسته‌بندی هاردباکس لوکس", "ارسال رایگان"]),
                new LoyaltyTier("vip", "VIP", 45000000, 2500, 15,
                [
                    "۱۵٪ تخفیف همیشگی روی تمام اقلام",
                    "مشاور اختصاصی و سفارشی‌سازی قالب پا در تبریز",
                    "ارسال اکسپرس هوایی کاملاً رایگان"
                ])
            );

            _db.LoyaltyRewards.AddRange(
                new LoyaltyReward("کد تخفیف ۱۰٪ اختصاصی", 350, "قابل استفاده روی تمام کفش‌های رسمی", "percent", 10, 30),
                new LoyaltyReward("کوپن تخفیف ۵۰۰ هزار تومانی", 600, "قابل استفاده روی سفارش‌های بالای ۴ میلیون تومان", "fixed", 500000, 45)
            );

            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded Loyalty Rules, Tiers and Rewards.");
        }

        // E. Seed Marketing Coupons & Campaigns
        if (!await _db.Coupons.AnyAsync(cancellationToken))
        {
            _db.Coupons.AddRange(
                new Coupon("VIP-AURA-15", "percent", 15, 1500000, 6000000, "1403-10-01", "1403-10-30", 100, 1, ["vip", "gold"]),
                new Coupon("CART-SAVE-5", "percent", 5, 500000, 2000000, "1403-10-01", "1404-10-01", 1000, 1, ["all"])
            );

            _db.Campaigns.Add(new Campaign(
                "کمپین یلدایی بوت‌های دست‌دوز اورا",
                "1403-09-25",
                "1403-10-05",
                clicksCount: 1420,
                ordersCount: 58,
                revenue: 412000000,
                spendCost: 35000000
            ));

            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded Marketing Coupons and Campaigns.");
        }

        // F. Seed CRM Customer & Order with Timeline
        if (!await _db.CustomerProfiles.AnyAsync(cancellationToken))
        {
            var customerUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == "customer@shop.local", cancellationToken);
            var customerId = customerUser?.Id ?? Guid.NewGuid();

            var profile = new CustomerProfile(
                customerId,
                "علیرضا رادمنش",
                "09121234567",
                "radmanesh.alireza@gmail.com",
                isVip: true,
                tier: "vip",
                totalSpent: 48500000,
                ordersCount: 7,
                walletBalance: 1200000,
                loyaltyPoints: 2680,
                preferredSize: 42,
                preferredColors: "قهوه‌ای تیره, کنیاکی"
            );

            _db.CustomerProfiles.Add(profile);

            _db.CustomerNotes.Add(new CustomerNote(
                profile.Id,
                "رضا تهرانی",
                "مشتری VIP قدیمی؛ تمایل به رنگ‌های عسلی و کنیاکی دارند و سالروز تولد ایشان ۲۵ بهمن است."
            ));

            _db.CustomerSegments.AddRange(
                new CustomerSegment("خریداران بوت چلسی زمستان", "مشتریانی با حداقل یک خرید بوت در فصل سرد", "category = 'boots' AND orders >= 1", 34),
                new CustomerSegment("باشگاه مشتریان VIP", "مشتریان با ارزش طول عمر بالاتر از ۴۵ میلیون تومان", "tier = 'vip'", 12)
            );

            // Seed sample order timeline
            var orderId = Guid.NewGuid();
            _db.OrderTimelineEvents.AddRange(
                new OrderTimelineEvent(orderId, "paid", "پرداخت موفق بانکی", "تراکنش به مبلغ ۶,۲۵۰,۰۰۰ تومان تایید شد.", "درگاه سامان", "۱۴۰۳/۱۰/۰۲ - ۱۰:۲۵"),
                new OrderTimelineEvent(orderId, "ready_to_ship", "بسته‌بندی لوکس و آماده ارسال", "محصول در هاردباکس اورا قرار گرفت.", "امیرحسین پارسا (انبار مرکزی)", "۱۴۰۳/۱۰/۰۲ - ۱۶:۴۵")
            );

            _db.ReturnRequests.Add(new ReturnRequest(
                orderId: orderId,
                orderNumber: "AUR-10482",
                customerId: profile.Id,
                customerName: "علیرضا رادمنش",
                customerPhone: "09121234567",
                reason: "سایز",
                requestedSize: 43,
                soleCondition: "سالم و تست شده فقط روی فرش",
                leatherCondition: "کاملاً نو بدون چروک",
                refundAmount: 6250000,
                status: "approved",
                adminNotes: "زیره تست شد و کاملاً سالم است؛ لنگه سایز ۴۳ برای ارسال رزرو شد."
            ));

            _db.AbandonedCartRecords.Add(new AbandonedCartRecord(
                cartId: Guid.NewGuid(),
                customerId: profile.Id,
                customerName: "مهرداد کاویانی",
                customerPhone: "09123456789",
                totalAmount: 6850000,
                itemCount: 1,
                lastActiveAt: DateTimeOffset.UtcNow.AddHours(-3)
            ));

            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded CRM Profile, Order Timeline, Returns and Abandoned Carts.");
        }

        // G. Seed CMS Homepage Blocks
        if (!await _db.CmsHomepageBlocks.AnyAsync(cancellationToken))
        {
            _db.CmsHomepageBlocks.AddRange(
                new CmsHomepageBlock("b-hero", "hero", true, 1),
                new CmsHomepageBlock("b-collections", "category_grid", true, 2),
                new CmsHomepageBlock("b-carousel", "product_carousel", true, 3)
            );
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded CMS Homepage Blocks.");
        }

        // H. Seed SEO Settings & Issues
        if (!await _db.SeoSettings.AnyAsync(cancellationToken))
        {
            _db.SeoSettings.Add(new SeoSetting());
            _db.SeoRedirects.Add(new SeoRedirect("/old-shoe", "/product/classic-oxford-cap-toe", 301));
            _db.SeoAuditIssues.Add(new SeoAuditIssue(
                "product",
                "کفش دابل مانک استرپ کنیاکی",
                "/product/monk-strap-double-cognac",
                "missing_alt",
                "medium",
                "تصویر گالری دوم فاقد متن جایگزین است."
            ));
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded SEO Settings and Audit Issues.");
        }

        // I. Seed Store Settings & Notifications
        if (!await _db.StoreSettings.AnyAsync(cancellationToken))
        {
            _db.StoreSettings.Add(new StoreSetting());

            _db.AdminNotifications.AddRange(
                new AdminNotification("سفارش جدید", "سفارش جدید شماره AUR-10482 ثبت و با موفقیت پرداخت شد.", "success"),
                new AdminNotification("هشدار موجودی انبار", "موجودی سایز ۴۵ کفش آکسفورد کلاسیک به اتمام رسیده است.", "warning")
            );

            _db.AuditLogs.Add(new AuditLog(
                adminId: "usr-1",
                adminName: "رضا تهرانی",
                adminRole: "Super Admin",
                action: "تغییر قیمت فروش محصول",
                entity: "کفش آکسفورد کلاسیک (AUR-OXF-001)",
                beforeValue: "۶,۵۰۰,۰۰۰ تومان",
                afterValue: "۶,۸۵۰,۰۰۰ تومان",
                ipAddress: "185.190.142.12",
                timestamp: "۱۴۰۳/۱۰/۰۲ - ۱۰:۴۰"
            ));

            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded Store Settings, Notifications and Audit Logs.");
        }
    }
}
