using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using Shop.Infrastructure.Persistence;

namespace Shop.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly ShopDbContext _db;

    public AuditLogService(ShopDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(
        string adminId,
        string adminName,
        string adminRole,
        string action,
        string entity,
        string beforeValue,
        string afterValue,
        string ipAddress,
        CancellationToken ct = default)
    {
        var log = new AuditLog(
            adminId,
            adminName,
            adminRole,
            action,
            entity,
            beforeValue,
            afterValue,
            ipAddress
        );

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }
}
