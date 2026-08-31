using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LicenseServer.Domain.Entities;
using LicenseServer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LicenseServer.Infrastructure.Services;

public record LicenseAdminLoginRequest(string Username, string Password);
public record LicenseAdminLoginResponse(string Token, string Username, string Email, string Role, DateTimeOffset ExpiresAt);

public interface ILicenseAuthService
{
    Task<LicenseAdminLoginResponse?> LoginAsync(LicenseAdminLoginRequest request, CancellationToken cancellationToken = default);
}

public class LicenseAuthService : ILicenseAuthService
{
    private readonly LicenseDbContext _db;
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;

    public LicenseAuthService(LicenseDbContext db, string? jwtSecret = null, string? issuer = null, string? audience = null)
    {
        _db = db;
        _jwtSecret = jwtSecret ?? "LICENSE_SERVER_ADMIN_JWT_SECRET_KEY_2026_SUPER_SECURE!";
        _jwtIssuer = issuer ?? "LicenseServer";
        _jwtAudience = audience ?? "LicenseAdmin";
    }

    public async Task<LicenseAdminLoginResponse?> LoginAsync(LicenseAdminLoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _db.AdminUsers.FirstOrDefaultAsync(u => u.Username == request.Username, cancellationToken);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSecret);
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            ]),
            Expires = expiresAt,
            Issuer = _jwtIssuer,
            Audience = _jwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return new LicenseAdminLoginResponse(
            tokenHandler.WriteToken(token),
            user.Username,
            user.Email,
            user.Role,
            expiresAt
        );
    }
}
