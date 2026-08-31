using LicenseServer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LicenseServer.Infrastructure.Data;

public class LicenseDbContext : DbContext
{
    public LicenseDbContext(DbContextOptions<LicenseDbContext> options) : base(options) { }

    public DbSet<ClientTenant> Tenants => Set<ClientTenant>();
    public DbSet<License> Licenses => Set<License>();
    public DbSet<LicenseUsageLog> UsageLogs => Set<LicenseUsageLog>();
    public DbSet<LicenseAdminUser> AdminUsers => Set<LicenseAdminUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ClientTenant>(builder =>
        {
            builder.ToTable("tenants");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.StoreName).IsRequired().HasMaxLength(200);
            builder.Property(t => t.OwnerName).IsRequired().HasMaxLength(200);
            builder.Property(t => t.ContactEmail).IsRequired().HasMaxLength(250);
            builder.Property(t => t.PhoneNumber).HasMaxLength(50);
            builder.Property(t => t.DomainOrHost).HasMaxLength(200);
        });

        modelBuilder.Entity<License>(builder =>
        {
            builder.ToTable("licenses");
            builder.HasKey(l => l.Id);
            builder.Property(l => l.LicenseKey).IsRequired().HasMaxLength(100);
            builder.HasIndex(l => l.LicenseKey).IsUnique();
            builder.Property(l => l.SecretKeyHash).IsRequired().HasMaxLength(256);
            builder.Property(l => l.Type).HasConversion<int>();
            builder.Property(l => l.Status).HasConversion<int>();

            builder.HasOne(l => l.Tenant)
                .WithMany(t => t.Licenses)
                .HasForeignKey(l => l.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LicenseUsageLog>(builder =>
        {
            builder.ToTable("license_usage_logs");
            builder.HasKey(l => l.Id);
            builder.Property(l => l.Action).IsRequired().HasMaxLength(100);
            builder.Property(l => l.Details).HasMaxLength(1000);
            builder.Property(l => l.ClientIp).HasMaxLength(100);

            builder.HasOne(l => l.License)
                .WithMany(l => l.UsageLogs)
                .HasForeignKey(l => l.LicenseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LicenseAdminUser>(builder =>
        {
            builder.ToTable("admin_users");
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Username).IsRequired().HasMaxLength(100);
            builder.HasIndex(u => u.Username).IsUnique();
            builder.Property(u => u.Email).IsRequired().HasMaxLength(200);
            builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(256);
            builder.Property(u => u.Role).IsRequired().HasMaxLength(50);
        });
    }
}
