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
                double.Parse(configuration["Jwt:ExpiryMinutes"] ?? "60")),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

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
