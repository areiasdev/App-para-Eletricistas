using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Users.DTOs;
using TecnicoApp.Domain.Enums;

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

        // Full name is always the acting user's own — but company branding belongs to
        // the team owner, and only Owner/Admin can change it (matches the useCanManage
        // gate used everywhere else this session).
        user.FullName = command.FullName;

        var ownerId = user.OwnerId ?? user.Id;
        var owner = ownerId == user.Id
            ? user
            : await db.Users.FirstOrDefaultAsync(u => u.Id == ownerId, cancellationToken);

        if (owner is null)
            return Result.Unauthorized();

        if (user.Role is UserRole.Owner or UserRole.Admin)
        {
            owner.CompanyName = string.IsNullOrWhiteSpace(command.CompanyName) ? null : command.CompanyName;
            owner.Nif = string.IsNullOrWhiteSpace(command.Nif) ? null : command.Nif;
            owner.Phone = string.IsNullOrWhiteSpace(command.Phone) ? null : command.Phone;
            owner.BrandColor = string.IsNullOrWhiteSpace(command.BrandColor) ? null : command.BrandColor;
            owner.Iban = string.IsNullOrWhiteSpace(command.Iban) ? null : command.Iban;
            owner.BankName = string.IsNullOrWhiteSpace(command.BankName) ? null : command.BankName;
            owner.DefaultHourlyRate = command.DefaultHourlyRate;
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new ProfileDto(
            user.Id,
            user.FullName,
            user.Email,
            owner.CompanyName,
            owner.Nif,
            owner.Phone,
            owner.LogoUrl,
            owner.BrandColor,
            owner.Iban,
            owner.BankName,
            owner.DefaultHourlyRate
        ));
    }
}
