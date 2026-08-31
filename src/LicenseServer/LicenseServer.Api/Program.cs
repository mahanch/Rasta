using System.Text;
using LicenseServer.Infrastructure.Data;
using LicenseServer.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// PostgreSQL DbContext
var connectionString = builder.Configuration.GetConnectionString("license-db") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Port=5432;Database=license_db;Username=postgres;Password=postgres";

builder.Services.AddDbContext<LicenseDbContext>(options =>
    options.UseNpgsql(connectionString));

// Domain & Infrastructure services
var cryptoSecret = builder.Configuration["LicenseSettings:CryptoSecret"] ?? "ENTERPRISE_LICENSE_MASTER_SECRET_KEY_2026_VERY_SECURE_KEY!";
builder.Services.AddSingleton<ILicenseCryptoService>(new LicenseCryptoService(cryptoSecret));

var jwtSecret = builder.Configuration["JwtSettings:Secret"] ?? "LICENSE_SERVER_ADMIN_JWT_SECRET_KEY_2026_SUPER_SECURE!";
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "LicenseServer";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "LicenseAdmin";

builder.Services.AddScoped<ILicenseAuthService>(sp => 
    new LicenseAuthService(sp.GetRequiredService<LicenseDbContext>(), jwtSecret, jwtIssuer, jwtAudience));
builder.Services.AddScoped<ILicenseManager, LicenseManager>();
builder.Services.AddScoped<LicenseDbSeeder>();

// JWT Authentication
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
builder.Services.AddControllers();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "License Management Server API",
        Version = "v1",
        Description = "Enterprise License Issuing & Verification System"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Seed initial database
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<LicenseDbSeeder>();
    await seeder.SeedAsync();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "License Server API v1");
    c.RoutePrefix = "swagger";
});

app.MapGet("/", () => Results.Redirect("/swagger"));

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
