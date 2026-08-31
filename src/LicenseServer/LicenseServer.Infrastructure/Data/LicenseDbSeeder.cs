using LicenseServer.Domain.Entities;
using LicenseServer.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LicenseServer.Infrastructure.Data;

public class LicenseDbSeeder
{
    private readonly LicenseDbContext _db;
    private readonly ILogger<LicenseDbSeeder> _logger;

    public LicenseDbSeeder(LicenseDbContext db, ILogger<LicenseDbSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await _db.Database.EnsureCreatedAsync(cancellationToken);

        // Seed Admin user
        if (!await _db.AdminUsers.AnyAsync(cancellationToken))
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123456");
            var admin = new LicenseAdminUser("admin", "admin@licenses.local", passwordHash, "Admin");
            _db.AdminUsers.Add(admin);
            _logger.LogInformation("Seeded default License Admin user.");
        }

        // Seed Demo Tenant & Demo License
        if (!await _db.Licenses.AnyAsync(l => l.LicenseKey == "SHOP-DEMO-LICENSE-KEY-2026", cancellationToken))
        {
            var tenant = new ClientTenant("Demo Store", "Ali Rezaei", "owner@demostore.com", "+989123456789", "localhost");
            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync(cancellationToken);

            var demoLicense = License.CreateOrderLimitLicense(tenant.Id, maxOrders: 100, customKey: "SHOP-DEMO-LICENSE-KEY-2026");
            _db.Licenses.Add(demoLicense);
            _db.UsageLogs.Add(new LicenseUsageLog(demoLicense.Id, "InitialSeed", "Demo license with 100 order limit seeded."));
            _logger.LogInformation("Seeded demo license SHOP-DEMO-LICENSE-KEY-2026.");
        }

        // Seed Lifetime Tenant & License
        if (!await _db.Licenses.AnyAsync(l => l.LicenseKey == "SHOP-LIFETIME-KEY-2026", cancellationToken))
        {
            var tenant2 = new ClientTenant("VIP Store", "Sara Mohammadi", "sara@vipstore.com", "+989987654321", "vipstore.com");
            _db.Tenants.Add(tenant2);
            await _db.SaveChangesAsync(cancellationToken);

            var lifetimeLicense = License.CreateLifetimeLicense(tenant2.Id, customKey: "SHOP-LIFETIME-KEY-2026");
            _db.Licenses.Add(lifetimeLicense);
            _db.UsageLogs.Add(new LicenseUsageLog(lifetimeLicense.Id, "InitialSeed", "Lifetime VIP license seeded."));
            _logger.LogInformation("Seeded lifetime license SHOP-LIFETIME-KEY-2026.");
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
