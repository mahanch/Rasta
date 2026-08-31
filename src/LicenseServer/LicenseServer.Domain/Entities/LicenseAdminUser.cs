namespace LicenseServer.Domain.Entities;

public class LicenseAdminUser
{
    public Guid Id { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Role { get; private set; } = "Admin";
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private LicenseAdminUser() { }

    public LicenseAdminUser(string username, string email, string passwordHash, string role = "Admin")
    {
        Id = Guid.NewGuid();
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
