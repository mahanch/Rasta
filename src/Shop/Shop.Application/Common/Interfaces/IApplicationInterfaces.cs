using Shop.Application.DTOs;
using Shop.Domain.ValueObjects;

namespace Shop.Application.Common.Interfaces;

public interface ILicenseClientService
{
    Task<LicenseState> GetOrRefreshLicenseStateAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
    Task<bool> CanProcessOrderAsync(CancellationToken cancellationToken = default);
    Task<bool> RecordOrderUsageAsync(CancellationToken cancellationToken = default);
    LicenseState GetCurrentLicenseState();
}

public interface IJwtTokenService
{
    AuthResponseDto GenerateTokens(Guid userId, string email, string fullName, string role);
}

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

public interface IEventPublisher
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
}

public interface IMongoReadDbContext
{
    MongoDB.Driver.IMongoCollection<T> GetCollection<T>(string collectionName);
}
