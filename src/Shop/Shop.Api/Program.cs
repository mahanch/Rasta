using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Repositories;
using Shop.Infrastructure.License;
using Shop.Infrastructure.Persistence;
using Shop.Infrastructure.Persistence.Write.Repositories;
using Shop.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// Aspire Service Defaults
builder.AddServiceDefaults();

// 1. Single PostgreSQL Database Context (Handles both CQRS Writes and Reads)
var shopDbConn = builder.Configuration.GetConnectionString("shop-db") 
    ?? builder.Configuration.GetConnectionString("shop-write-db")
    ?? builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Port=5432;Database=shop_db;Username=postgres;Password=postgres";

builder.Services.AddDbContext<ShopDbContext>(options =>
    options.UseNpgsql(shopDbConn));

builder.Services.AddScoped<IShopDbContext>(sp => sp.GetRequiredService<ShopDbContext>());

// 2. Repositories & Unit of Work (Commands / Domain Persistence)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IBlogRepository, BlogRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAuditLogService, Shop.Infrastructure.Services.AuditLogService>();

// 3. Security & JWT
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

var jwtSecret = builder.Configuration["JwtSettings:Secret"] ?? "SHOP_ENTERPRISE_JWT_SUPER_SECRET_KEY_2026_VERY_SECURE!";
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "ShopApi";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "ShopClients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };
});

builder.Services.AddAuthorization();

// 4. MediatR (CQRS Pipeline: Commands and Direct-DB Queries)
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Shop.Application.DTOs.AuthResponseDto).Assembly);
});

// 5. License Client Service & Background Sync
var licenseServerUrl = builder.Configuration["LicenseSettings:LicenseServerUrl"] ?? "http://localhost:5100";
builder.Services.AddHttpClient<ILicenseClientService, LicenseClientService>(client =>
{
    client.BaseAddress = new Uri(licenseServerUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHostedService<LicenseSyncWorker>();

// 6. Database Seeder
builder.Services.AddScoped<ShopDatabaseSeeder>();

// 7. Web & Swagger
builder.Services.AddControllers(options =>
{
    options.Filters.Add<Shop.Api.Filters.AdminApiResponseFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Aura Leather Admin API Specification",
        Version = "v1.0.0",
        Description = "سند جامع مشخصات فنی APIهای پنل مدیریت چرم اورا (۱۴ ماژول ادمین، هوش تجاری، ماتریس انبارداری ۳۹ تا ۴۵، تایملاین کارگاه و لاگ تغییرات)."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter token directly or with 'Bearer ' prefix.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }

    var appXmlPath = Path.Combine(AppContext.BaseDirectory, "Shop.Application.xml");
    if (File.Exists(appXmlPath))
    {
        c.IncludeXmlComments(appXmlPath);
    }
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Seed database on startup
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<ShopDatabaseSeeder>();
    await seeder.SeedAsync();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Shop API v1");
    c.RoutePrefix = "swagger";
});

app.MapGet("/", () => Results.Redirect("/swagger"));

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<Shop.Api.Middleware.LicenseValidationMiddleware>();
app.MapControllers();

app.Run();
