using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Auth.DTOs;

namespace TecnicoApp.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler(
    IAppDbContext db,
    ITokenService tokenService)
    : IRequestHandler<LoginCommand, Result<AuthResponseDto>>
{
    public async Task<Result<AuthResponseDto>> Handle(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == command.Email.ToLowerInvariant(), cancellationToken);

        // Mensagem genérica — não revelar se o email existe ou não
        if (user is null || !BCrypt.Net.BCrypt.Verify(command.Password, user.PasswordHash))
            return Result.Unauthorized();

        var refreshToken = tokenService.GenerateRefreshToken();
        user.RefreshTokenHash = tokenService.HashRefreshToken(refreshToken);
        user.RefreshTokenExpiresAt = DateTime.UtcNow.Add(tokenService.RefreshTokenLifetime);
        user.ModifiedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        var accessToken = tokenService.GenerateAccessToken(user);

        // Branding always reflects the team owner, not whoever is logging in.
        var owner = user.OwnerId is null
            ? user
            : await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.OwnerId, cancellationToken);

        return Result.Success(new AuthResponseDto(
            accessToken,
            refreshToken,
            user.RefreshTokenExpiresAt!.Value,
            new UserDto(user.Id, user.FullName, user.Email, user.Role, owner?.CompanyName, owner?.LogoUrl, owner?.BrandColor)
        ));
    }
}
