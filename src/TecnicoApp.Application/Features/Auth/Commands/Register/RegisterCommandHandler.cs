using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Auth.DTOs;
using TecnicoApp.Domain.Entities;

namespace TecnicoApp.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandHandler(
    IAppDbContext db,
    ITokenService tokenService,
    IAppSettings appSettings)
    : IRequestHandler<RegisterCommand, Result<AuthResponseDto>>
{
    public async Task<Result<AuthResponseDto>> Handle(
        RegisterCommand command,
        CancellationToken cancellationToken)
    {
        // One install = one company. Once the first account (the Owner) exists, everyone else
        // joins through a team invite — otherwise anyone who finds the URL could create a
        // separate account on this company's server.
        if (!appSettings.AllowOpenRegistration && await db.Users.AnyAsync(cancellationToken))
            return Result.Forbidden("O registo está fechado. Pede um convite ao administrador da tua empresa.");

        var emailExists = await db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == command.Email.ToLowerInvariant(), cancellationToken);

        if (emailExists)
            return Result.Conflict("Já existe uma conta com este email.");

        var refreshToken = tokenService.GenerateRefreshToken();
        var user = new User
        {
            Email = command.Email.ToLowerInvariant(),
            FullName = command.FullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(command.Password),
            RefreshTokenHash = tokenService.HashRefreshToken(refreshToken),
            RefreshTokenExpiresAt = DateTime.UtcNow.Add(tokenService.RefreshTokenLifetime),
        };

        await db.Users.AddAsync(user, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var accessToken = tokenService.GenerateAccessToken(user);

        return Result.Success(new AuthResponseDto(
            accessToken,
            refreshToken,
            user.RefreshTokenExpiresAt!.Value,
            new UserDto(user.Id, user.FullName, user.Email, user.Role, user.CompanyName, user.LogoUrl, user.BrandColor)
        ));
    }
}
