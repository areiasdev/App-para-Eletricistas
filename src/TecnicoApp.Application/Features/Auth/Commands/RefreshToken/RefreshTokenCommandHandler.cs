using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Auth.DTOs;

namespace TecnicoApp.Application.Features.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler(
    IAppDbContext db,
    ITokenService tokenService)
    : IRequestHandler<RefreshTokenCommand, Result<AuthResponseDto>>
{
    public async Task<Result<AuthResponseDto>> Handle(
        RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        var incomingHash = tokenService.HashRefreshToken(command.RefreshToken);
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.RefreshTokenHash == incomingHash, cancellationToken);

        if (user is null || user.RefreshTokenExpiresAt is null || user.RefreshTokenExpiresAt < DateTime.UtcNow)
            return Result.Unauthorized();

        var newRefreshToken = tokenService.GenerateRefreshToken();
        user.RefreshTokenHash = tokenService.HashRefreshToken(newRefreshToken);
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(30);
        user.ModifiedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        var accessToken = tokenService.GenerateAccessToken(user);

        // Branding always reflects the team owner, not whoever is refreshing.
        var owner = user.OwnerId is null
            ? user
            : await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.OwnerId, cancellationToken);

        return Result.Success(new AuthResponseDto(
            accessToken,
            newRefreshToken,
            user.RefreshTokenExpiresAt!.Value,
            new UserDto(user.Id, user.FullName, user.Email, user.Role, owner?.CompanyName, owner?.LogoUrl, owner?.BrandColor)
        ));
    }
}
