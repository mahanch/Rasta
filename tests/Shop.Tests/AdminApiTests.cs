using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shop.Api.Controllers.Admin;
using Shop.Api.Filters;
using Shop.Application.Common.Models;
using Shop.Application.DTOs;
using Shop.Domain.Entities;
using Shop.Infrastructure.Persistence;
using Shop.Infrastructure.Security;
using Shop.Infrastructure.Services;
using Xunit;

namespace Shop.Tests.Admin;

public class AdminApiTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ShopDbContext _db;
    private readonly PasswordHasher _passwordHasher;
    private readonly JwtTokenService _jwtTokenService;
    private readonly AuditLogService _auditLogService;

    public AdminApiTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new ShopDbContext(options);
        _db.Database.EnsureCreated();

        _passwordHasher = new PasswordHasher();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Secret"] = "SHOP_ENTERPRISE_JWT_SUPER_SECRET_KEY_2026_VERY_SECURE!",
                ["JwtSettings:Issuer"] = "ShopApi",
                ["JwtSettings:Audience"] = "ShopClients",
                ["JwtSettings:ExpiryMinutes"] = "120"
            })
            .Build();

        _jwtTokenService = new JwtTokenService(config);
        _auditLogService = new AuditLogService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private static ControllerContext CreateControllerContext(string userId, string email, string name, string role, List<string>? permissions = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, name),
            new(ClaimTypes.Role, role)
        };

        if (permissions != null)
        {
            foreach (var p in permissions) claims.Add(new Claim("permission", p));
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        return new ControllerContext { HttpContext = httpContext };
    }

    [Fact]
    public async Task AdminLogin_ShouldReturnValidToken_WithPermissionsAndRoleFa()
    {
        // Arrange
        var superAdmin = new User("r.tehrani@aura-leather.ir", "رضا تهرانی", _passwordHasher.HashPassword("StrongPassword!123"), "super_admin", "09121234567");
        _db.Users.Add(superAdmin);
        _db.AdminRoles.Add(new AdminRole("super_admin", "مدیر ارشد پلتفرم", "دسترسی کامل", ["*"]));
        await _db.SaveChangesAsync();

        var controller = new AdminAuthController(_db, _passwordHasher, _jwtTokenService);

        // Act
        var result = await controller.Login(new AdminLoginRequest("r.tehrani@aura-leather.ir", "StrongPassword!123"), CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)result;
        var response = ok.Value as ApiResponse<AdminLoginResponse>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.User.Email.Should().Be("r.tehrani@aura-leather.ir");
        response.Data.User.Role.Should().Be("super_admin");
        response.Data.User.RoleNameFa.Should().Be("مدیر ارشد پلتفرم");
        response.Data.User.Permissions.Should().Contain("*");
        response.Data.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task FootwearCatalogAndInventoryMatrix_ShouldManageStockAcrossSizes39To45()
    {
        // Arrange
        var product = new FootwearProduct(
            persianName: "کفش آکسفورد کلاسیک نوک کلاهدار",
            name: "Classic Oxford Cap-Toe",
            slug: "classic-oxford-cap-toe",
            sku: "AUR-OXF-001",
            category: "formal",
            categoryName: "کفش رسمی",
            collection: "کفش‌های شاهکار",
            gender: "men",
            basePrice: 6850000,
            discountPrice: 6250000,
            costPrice: 3400000,
            specs: new FootwearSpecs
            {
                Material = "چرم تمام‌دانه دباغی تبریز",
                SoleMaterial = "زیره دانیت",
                Construction = "گودیر ولتد"
            },
            seo: new FootwearSeo { Title = "کفش آکسفورد" },
            variants:
            [
                new FootwearVariant(39, "قهوه‌ای", "#3B2314", "AUR-OXF-001-39", 2, 3),
                new FootwearVariant(40, "قهوه‌ای", "#3B2314", "AUR-OXF-001-40", 5, 3),
                new FootwearVariant(41, "قهوه‌ای", "#3B2314", "AUR-OXF-001-41", 4, 3),
                new FootwearVariant(42, "قهوه‌ای", "#3B2314", "AUR-OXF-001-42", 8, 3),
                new FootwearVariant(43, "قهوه‌ای", "#3B2314", "AUR-OXF-001-43", 3, 3),
                new FootwearVariant(44, "قهوه‌ای", "#3B2314", "AUR-OXF-001-44", 2, 3),
                new FootwearVariant(45, "قهوه‌ای", "#3B2314", "AUR-OXF-001-45", 0, 3)
            ],
            images: ["img.jpg"],
            shortDescription: "کفش دست‌دوز تبریز",
            fullDescription: "توضیحات کامل",
            careInstructions: ["قالب سدر"]
        );

        _db.FootwearProducts.Add(product);
        await _db.SaveChangesAsync();

        var controller = new AdminInventoryController(_db, _auditLogService)
        {
            ControllerContext = CreateControllerContext("usr-1", "r.tehrani@aura-leather.ir", "رضا تهرانی", "super_admin")
        };

        var variant45 = product.Variants.First(v => v.Size == 45);

        // Act: Update stock of size 45 from 0 to 5
        var result = await controller.UpdateVariantStock(
            product.Id.ToString(),
            variant45.Id.ToString(),
            new UpdateVariantStockRequest(5, "افزایش موجودی دوخته شده توسط کارگاه تبریز"),
            CancellationToken.None
        );

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var updatedVariant = await _db.FootwearVariants.FindAsync(variant45.Id);
        updatedVariant.Should().NotBeNull();
        updatedVariant!.Stock.Should().Be(5);
        updatedVariant.StockStatus.Should().Be("in_stock");

        // Verify Audit Log
        var log = await _db.AuditLogs.FirstOrDefaultAsync(l => l.Entity.Contains("سایز 45"));
        log.Should().NotBeNull();
        log!.AdminName.Should().Be("رضا تهرانی");
        log.AfterValue.Should().Contain("5 جفت");
    }

    [Fact]
    public async Task OrderStatusChange_ShouldAppendTimelineNodeAndCreateAuditLog()
    {
        // Arrange
        var customer = new User("customer@aura-leather.ir", "علیرضا رادمنش", "hash", "Customer", "09121234567");
        _db.Users.Add(customer);
        await _db.SaveChangesAsync();

        var address = new Shop.Domain.ValueObjects.Address("زعفرانیه", "تهران", "تهران", "12345", "ایران", "علیرضا رادمنش", "09121234567");
        var items = new List<(Guid productId, string productName, string sku, decimal unitPrice, int quantity, string? imageUrl)>
        {
            (Guid.NewGuid(), "کفش آکسفورد", "AUR-OXF-001-42", 6250000m, 1, "img.jpg")
        };

        var order = Order.Create(customer.Id, address, items);
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var controller = new AdminOrdersController(_db, _auditLogService)
        {
            ControllerContext = CreateControllerContext("usr-1", "r.tehrani@aura-leather.ir", "رضا تهرانی", "super_admin")
        };

        // Act: Update status to shipped
        var result = await controller.UpdateOrderStatus(
            order.Id.ToString(),
            new UpdateAdminOrderStatusRequest("shipped", "مرسوله تحویل مامور توزیع پست پیشتاز گردید.", "PST-98234120-IR"),
            CancellationToken.None
        );

        // Assert
        result.Should().BeOfType<OkObjectResult>();

        var timelineEvents = await _db.OrderTimelineEvents.Where(e => e.OrderId == order.Id).ToListAsync();
        timelineEvents.Should().HaveCount(1);
        timelineEvents[0].Status.Should().Be("shipped");
        timelineEvents[0].PerformedBy.Should().Be("رضا تهرانی");
        timelineEvents[0].Description.Should().Contain("PST-98234120-IR");

        var auditLog = await _db.AuditLogs.FirstOrDefaultAsync(l => l.Action == "تغییر وضعیت سفارش");
        auditLog.Should().NotBeNull();
        auditLog!.AfterValue.Should().Be("shipped");
    }

    [Fact]
    public async Task SeoRedirect_WhenFromAndToAreEqual_ShouldDetectLoopAndReturn422()
    {
        // Arrange
        var controller = new AdminSeoController(_db);

        // Act
        var result = await controller.CreateRedirect(
            new CreateSeoRedirectRequest("/product/classic-oxford", "/product/classic-oxford", 301),
            CancellationToken.None
        );

        // Assert
        result.Should().BeOfType<UnprocessableEntityObjectResult>();
        var obj = (UnprocessableEntityObjectResult)result;
        var error = obj.Value as ApiErrorResponse;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be(StandardErrorCodes.RedirectLoopDetected);
        error.StatusCode.Should().Be(422);
    }

    [Fact]
    public async Task ReportsExport_ShouldGenerateCsvWithUtf8Bom()
    {
        // Arrange
        var product = new FootwearProduct(
            persianName: "کفش دست‌دوز تبریز",
            name: "Tabriz Shoe",
            slug: "tabriz-shoe",
            sku: "AUR-TBZ-001",
            category: "formal",
            categoryName: "رسمی",
            collection: "شاهکار",
            gender: "men",
            basePrice: 5000000,
            discountPrice: 4500000,
            costPrice: 2500000,
            specs: new FootwearSpecs(),
            seo: new FootwearSeo(),
            variants: [],
            images: [],
            shortDescription: "توضیح",
            fullDescription: "توضیح کامل",
            careInstructions: []
        );

        _db.FootwearProducts.Add(product);
        await _db.SaveChangesAsync();

        var controller = new AdminReportsController(_db)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        // Act
        var result = await controller.ExportData("products", "csv", CancellationToken.None);

        // Assert
        result.Should().BeOfType<FileContentResult>();
        var fileResult = (FileContentResult)result;
        fileResult.ContentType.Should().Be("text/csv; charset=utf-8");

        // Verify UTF-8 BOM preamble (0xEF, 0xBB, 0xBF)
        fileResult.FileContents.Length.Should().BeGreaterThan(3);
        fileResult.FileContents[0].Should().Be(0xEF);
        fileResult.FileContents[1].Should().Be(0xBB);
        fileResult.FileContents[2].Should().Be(0xBF);
    }

    [Fact]
    public async Task RequirePermissionFilter_WhenUserLacksPermission_ShouldReturn403()
    {
        // Arrange
        var attribute = new RequirePermissionAttribute("products.delete");
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "usr-2"),
            new(ClaimTypes.Role, "support"),
            new("permission", "orders.read")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var authContext = new AuthorizationFilterContext(actionContext, []);

        // Act
        await attribute.OnAuthorizationAsync(authContext);

        // Assert
        authContext.Result.Should().BeOfType<ObjectResult>();
        var objResult = (ObjectResult)authContext.Result!;
        objResult.StatusCode.Should().Be(403);
        var error = objResult.Value as ApiErrorResponse;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be(StandardErrorCodes.ForbiddenPermission);
    }
}
