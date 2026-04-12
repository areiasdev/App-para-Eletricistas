using TecnicoApp.Domain.Entities;

namespace TecnicoApp.Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
