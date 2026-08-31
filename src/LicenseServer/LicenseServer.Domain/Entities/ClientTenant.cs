namespace LicenseServer.Domain.Entities;

public class ClientTenant
{
    public Guid Id { get; private set; }
    public string StoreName { get; private set; } = string.Empty;
    public string OwnerName { get; private set; } = string.Empty;
    public string ContactEmail { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public string? DomainOrHost { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public List<License> Licenses { get; private set; } = [];

    private ClientTenant() { }

    public ClientTenant(string storeName, string ownerName, string contactEmail, string? phoneNumber = null, string? domainOrHost = null)
    {
        Id = Guid.NewGuid();
        StoreName = storeName;
        OwnerName = ownerName;
        ContactEmail = contactEmail;
        PhoneNumber = phoneNumber;
        DomainOrHost = domainOrHost;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateInfo(string storeName, string ownerName, string contactEmail, string? phoneNumber, string? domainOrHost)
    {
        StoreName = storeName;
        OwnerName = ownerName;
        ContactEmail = contactEmail;
        PhoneNumber = phoneNumber;
        DomainOrHost = domainOrHost;
    }
}
