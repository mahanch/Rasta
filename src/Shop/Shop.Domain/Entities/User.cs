using Shop.Domain.Common;

namespace Shop.Domain.Entities;

public class User : AggregateRoot<Guid>
{
    public string Email { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Role { get; private set; } = "Customer"; // "Admin" or "Customer"
    public string? PhoneNumber { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; private set; }

    public List<RefreshToken> RefreshTokens { get; private set; } = [];

    private User() { }

    public User(string email, string fullName, string passwordHash, string role = "Customer", string? phoneNumber = null)
    {
        Email = email.Trim().ToLowerInvariant();
        FullName = fullName.Trim();
        PasswordHash = passwordHash;
        Role = role;
        PhoneNumber = phoneNumber;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void AddRefreshToken(string token, DateTimeOffset expiresAt)
    {
        RefreshTokens.Add(new RefreshToken(token, Id, expiresAt));
    }

    public void RevokeRefreshToken(string token)
    {
        var rt = RefreshTokens.FirstOrDefault(t => t.Token == token);
        rt?.Revoke();
    }

    public void UpdateProfile(string fullName, string? phoneNumber)
    {
        FullName = fullName.Trim();
        PhoneNumber = phoneNumber;
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTimeOffset.UtcNow;
    }
}

public class RefreshToken : Entity<Guid>
{
    public string Token { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private RefreshToken() { }

    public RefreshToken(string token, Guid userId, DateTimeOffset expiresAt)
    {
        Token = token;
        UserId = userId;
        ExpiresAt = expiresAt;
        IsRevoked = false;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public bool IsActive => !IsRevoked && ExpiresAt > DateTimeOffset.UtcNow;

    public void Revoke()
    {
        IsRevoked = true;
    }
}
