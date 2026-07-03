using TecnicoApp.Domain.Entities;

namespace TecnicoApp.Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string GeneratePortalToken(Guid clientId, Guid ownerId, string clientName, string clientEmail);
}
