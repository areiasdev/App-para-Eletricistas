using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Auth.DTOs;
using TecnicoApp.Domain.Entities;

namespace TecnicoApp.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandHandler(
    IAppDbContext db,
    ITokenService tokenService)
    : IRequestHandler<RegisterCommand, Result<AuthResponseDto>>
{
    public async Task<Result<AuthResponseDto>> Handle(
        RegisterCommand command,
        CancellationToken cancellationToken)
    {
        var emailExists = await db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == command.Email.ToLowerInvariant(), cancellationToken);

        if (emailExists)
            return Result.Conflict("Já existe uma conta com este email.");

        var user = new User
        {
            Email = command.Email.ToLowerInvariant(),
            FullName = command.FullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(command.Password),
            RefreshToken = tokenService.GenerateRefreshToken(),
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(30),
        };

        await db.Users.AddAsync(user, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var accessToken = tokenService.GenerateAccessToken(user);

        return Result.Success(new AuthResponseDto(
            accessToken,
            user.RefreshToken!,
            user.RefreshTokenExpiresAt!.Value,
            new UserDto(user.Id, user.FullName, user.Email)
        ));
    }
}
