using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Equipment.Commands.DeleteEquipment;

public class DeleteEquipmentCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<DeleteEquipmentCommand, Result>
{
    public async Task<Result> Handle(
        DeleteEquipmentCommand request, CancellationToken cancellationToken)
    {
        // Resolve ownerId: team members share their owner's clients/equipment
        var caller = await db.Users.AsNoTracking()
            .Where(u => u.Id == currentUser.UserId)
            .Select(u => new { OwnerId = u.OwnerId ?? u.Id, u.Role })
            .FirstOrDefaultAsync(cancellationToken);

        if (caller is null)
            return Result.Unauthorized();

        // Deletes are irreversible from the UI — restrict to Owner/Admin so a technician
        // can't wipe out company records unsupervised.
        if (caller.Role is not (UserRole.Owner or UserRole.Admin))
            return Result.Forbidden("Apenas o proprietário ou administradores podem apagar equipamentos.");

        var ownerId = caller.OwnerId;

        var equipment = await db.Equipment
            .Include(e => e.Client)
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (equipment is null)
            return Result.NotFound();

        if (equipment.Client.UserId != ownerId)
            return Result.Forbidden();

        equipment.IsDeleted = true;
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
