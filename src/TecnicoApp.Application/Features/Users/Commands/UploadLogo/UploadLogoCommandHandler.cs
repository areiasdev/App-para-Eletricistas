using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Common.Security;
using TecnicoApp.Application.Features.Users.DTOs;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Users.Commands.UploadLogo;

public sealed class UploadLogoCommandHandler(
    IAppDbContext db,
    ICurrentUserService currentUser,
    IFileStorageService fileStorage)
    : IRequestHandler<UploadLogoCommand, Result<ProfileDto>>
{
    public async Task<Result<ProfileDto>> Handle(UploadLogoCommand command, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == currentUser.UserId, cancellationToken);

        if (user is null)
            return Result.NotFound("Utilizador não encontrado.");

        if (user.Role is not (UserRole.Owner or UserRole.Admin))
            return Result.Forbidden("Apenas o proprietário ou administradores podem alterar o logótipo.");

        // The logo belongs to the company, not the individual who uploaded it.
        var ownerId = user.OwnerId ?? user.Id;
        var owner = ownerId == user.Id
            ? user
            : await db.Users.FirstOrDefaultAsync(u => u.Id == ownerId, cancellationToken);

        if (owner is null)
            return Result.Unauthorized();

        var extension = ImageFiles.ExtensionsByContentType[command.ContentType];
        var url = await fileStorage.SaveLogoAsync(ownerId, command.FileContent, extension, cancellationToken);

        owner.LogoUrl = url;
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
