using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Users.DTOs;

namespace TecnicoApp.Application.Features.Users.Queries.GetProfile;

public sealed class GetProfileQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetProfileQuery, Result<ProfileDto>>
{
    public async Task<Result<ProfileDto>> Handle(
        GetProfileQuery request,
        CancellationToken cancellationToken)
    {
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == currentUser.UserId, cancellationToken);

        if (user is null)
            return Result.NotFound("Utilizador não encontrado.");

        // Company branding always reflects the team owner — a technician's own row has
        // no company identity of its own, only the account fields (name/email) are theirs.
        var owner = user.OwnerId is null
            ? user
            : await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.OwnerId, cancellationToken);

        return Result.Success(new ProfileDto(
            user.Id,
            user.FullName,
            user.Email,
            owner?.CompanyName,
            owner?.Nif,
            owner?.Phone,
            owner?.LogoUrl,
            owner?.BrandColor
        ));
    }
}
