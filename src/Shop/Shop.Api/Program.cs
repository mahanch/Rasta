using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Repositories;
using Shop.Infrastructure.License;
using Shop.Infrastructure.Messaging;
using Shop.Infrastructure.Messaging.Consumers;
using Shop.Infrastructure.Persistence;
using Shop.Infrastructure.Persistence.Read;
using Shop.Infrastructure.Persistence.Write;
using Shop.Infrastructure.Persistence.Write.Repositories;
using Shop.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// Aspire Service Defaults
builder.AddServiceDefaults();

// 1. PostgreSQL Write Database Context
var writeDbConn = builder.Configuration.GetConnectionString("shop-write-db") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Port=5432;Database=shop_write_db;Username=postgres;Password=postgres";

builder.Services.AddDbContext<ShopWriteDbContext>(options =>
    options.UseNpgsql(writeDbConn));

// 2. MongoDB Read Database Context
builder.Services.AddSingleton<IMongoReadDbContext, MongoReadDbContext>();

// 3. Write Repositories & Unit of Work
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IBlogRepository, BlogRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// 4. Security & JWT
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

// 5. MediatR (CQRS Pipeline)
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Shop.Application.DTOs.AuthResponseDto).Assembly);
});

// 6. MassTransit & RabbitMQ Event Bus
var rabbitMqConn = builder.Configuration.GetConnectionString("messaging") 
    ?? builder.Configuration["RabbitMQ:ConnectionString"];

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ProductProjectionConsumer>();
    x.AddConsumer<CategoryProjectionConsumer>();
    x.AddConsumer<OrderProjectionConsumer>();
    x.AddConsumer<BlogPostProjectionConsumer>();
    x.AddConsumer<BlogCategoryProjectionConsumer>();

    if (!string.IsNullOrWhiteSpace(rabbitMqConn) && rabbitMqConn.StartsWith("amqp", StringComparison.OrdinalIgnoreCase))
    {
        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host(new Uri(rabbitMqConn));
            cfg.ConfigureEndpoints(context);
        });
    }
    else
    {
        // Fallback to high-performance in-memory bus if rabbitmq connection is not configured or during local standalone testing
        x.UsingInMemory((context, cfg) =>
        {
            cfg.ConfigureEndpoints(context);
        });
    }
});

builder.Services.AddScoped<IEventPublisher, EventPublisher>();

// 7. License Client Service & Background Sync
var licenseServerUrl = builder.Configuration["LicenseSettings:LicenseServerUrl"] ?? "http://localhost:5100";
builder.Services.AddHttpClient<ILicenseClientService, LicenseClientService>(client =>
{
    client.BaseAddress = new Uri(licenseServerUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHostedService<LicenseSyncWorker>();

// 8. Seeder
builder.Services.AddScoped<ShopDatabaseSeeder>();

// 9. Web & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Enterprise Shop API (DDD + CQRS)",
        Version = "v1",
        Description = "Enterprise-Grade Dynamic Shop MVP with Separated Read (Mongo) & Write (PostgreSQL), RabbitMQ, and License Protection."
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

    // Initial License verification
    var licenseService = scope.ServiceProvider.GetRequiredService<ILicenseClientService>();
    await licenseService.GetOrRefreshLicenseStateAsync(forceRefresh: true);
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
