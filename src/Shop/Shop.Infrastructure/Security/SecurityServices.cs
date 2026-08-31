using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Shop.Application.Common.Interfaces;
using Shop.Application.DTOs;

namespace Shop.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);
    public bool VerifyPassword(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiryMinutes;

    public JwtTokenService(IConfiguration configuration)
    {
        _secret = configuration["JwtSettings:Secret"] ?? "SHOP_ENTERPRISE_JWT_SUPER_SECRET_KEY_2026_VERY_SECURE!";
        _issuer = configuration["JwtSettings:Issuer"] ?? "ShopApi";
        _audience = configuration["JwtSettings:Audience"] ?? "ShopClients";
        _expiryMinutes = int.TryParse(configuration["JwtSettings:ExpiryMinutes"], out var exp) ? exp : 120;
    }

    public AuthResponseDto GenerateTokens(Guid userId, string email, string fullName, string role)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secret);
        var expiresAt = DateTime.UtcNow.AddMinutes(_expiryMinutes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, fullName),
                new Claim(ClaimTypes.Role, role)
            ]),
            Expires = expiresAt,
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwtString = tokenHandler.WriteToken(token);
        var refreshToken = $"RT-{Guid.NewGuid():N}"[..32].ToUpperInvariant();

        return new AuthResponseDto(
            jwtString,
            refreshToken,
            userId,
            email,
            fullName,
            role,
            expiresAt
        );
    }
}
