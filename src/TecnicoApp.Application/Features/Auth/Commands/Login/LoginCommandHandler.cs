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

        user.RefreshToken = tokenService.GenerateRefreshToken();
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(30);
        user.ModifiedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        var accessToken = tokenService.GenerateAccessToken(user);

        return Result.Success(new AuthResponseDto(
            accessToken,
            user.RefreshToken!,
            user.RefreshTokenExpiresAt!.Value,
            new UserDto(user.Id, user.FullName, user.Email, user.Plan.ToString())
        ));
    }
}
