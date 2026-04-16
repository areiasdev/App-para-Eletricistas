using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Users.DTOs;

namespace TecnicoApp.Application.Features.Users.Commands.UpdateProfile;

public sealed class UpdateProfileCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpdateProfileCommand, Result<ProfileDto>>
{
    public async Task<Result<ProfileDto>> Handle(
        UpdateProfileCommand command,
        CancellationToken cancellationToken)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == currentUser.UserId, cancellationToken);

        if (user is null)
            return Result.NotFound("Utilizador não encontrado.");

        user.FullName = command.FullName;
        user.CompanyName = string.IsNullOrWhiteSpace(command.CompanyName) ? null : command.CompanyName;
        user.Nif = string.IsNullOrWhiteSpace(command.Nif) ? null : command.Nif;
        user.Phone = string.IsNullOrWhiteSpace(command.Phone) ? null : command.Phone;
        user.LogoUrl = string.IsNullOrWhiteSpace(command.LogoUrl) ? null : command.LogoUrl;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new ProfileDto(
            user.Id,
            user.FullName,
            user.Email,
            user.CompanyName,
            user.Nif,
            user.Phone,
            user.LogoUrl,
            user.Plan.ToString()
        ));
    }
}
