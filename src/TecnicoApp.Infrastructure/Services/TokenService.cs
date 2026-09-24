using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Domain.Entities;

namespace TecnicoApp.Infrastructure.Services;

public sealed class TokenService(IConfiguration configuration) : ITokenService
{
    public string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
        };

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                double.Parse(configuration["Jwt:ExpiryMinutes"] ?? "60", System.Globalization.CultureInfo.InvariantCulture)),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private const int DefaultRefreshTokenDays = 30;

    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(
        int.TryParse(configuration["Jwt:RefreshTokenDays"], out var days) && days > 0 ? days : DefaultRefreshTokenDays);

    public string GenerateRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    // Refresh tokens are high-entropy random values (not passwords), so an unsalted
    // hash is fine — same reasoning already applied to team-invite tokens elsewhere in
    // this codebase. Hashing at rest means a DB leak alone can't be used to hijack sessions.
    public string HashRefreshToken(string rawToken)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    public string GeneratePortalToken(Guid clientId, Guid ownerId, string clientName, string clientEmail, int tokenVersion)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, clientId.ToString()),
            new Claim(ClaimTypes.Role, "ClientPortal"),
            new Claim("ownerId", ownerId.ToString()),
            new Claim(ClaimTypes.Name, clientName),
            new Claim(ClaimTypes.Email, clientEmail),
            new Claim("portalTokenVersion", tokenVersion.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
