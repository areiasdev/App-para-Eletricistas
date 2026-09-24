using TecnicoApp.Domain.Entities;

namespace TecnicoApp.Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();

    /// <summary>How long a refresh token (and so a "remember me" session) stays valid (Jwt:RefreshTokenDays).</summary>
    TimeSpan RefreshTokenLifetime { get; }
    string HashRefreshToken(string rawToken);
    string GeneratePortalToken(Guid clientId, Guid ownerId, string clientName, string clientEmail, int tokenVersion);
}
