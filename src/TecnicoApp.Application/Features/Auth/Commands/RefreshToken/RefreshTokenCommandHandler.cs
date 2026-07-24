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
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == command.RefreshToken, cancellationToken);

        if (user is null || user.RefreshTokenExpiresAt is null || user.RefreshTokenExpiresAt < DateTime.UtcNow)
            return Result.Unauthorized();

        user.RefreshToken = tokenService.GenerateRefreshToken();
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(30);
        user.ModifiedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        var accessToken = tokenService.GenerateAccessToken(user);

        return Result.Success(new AuthResponseDto(
            accessToken,
            user.RefreshToken!,
            user.RefreshTokenExpiresAt!.Value,
            new UserDto(user.Id, user.FullName, user.Email, user.Role)
        ));
    }
}
