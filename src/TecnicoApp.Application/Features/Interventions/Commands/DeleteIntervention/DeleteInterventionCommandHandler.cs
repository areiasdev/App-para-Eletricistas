using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Interventions.Commands.DeleteIntervention;

public class DeleteInterventionCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<DeleteInterventionCommand, Result>
{
    public async Task<Result> Handle(
        DeleteInterventionCommand request, CancellationToken cancellationToken)
    {
        // Resolve ownerId: team members share their owner's interventions
        var caller = await db.Users.AsNoTracking()
            .Where(u => u.Id == currentUser.UserId)
            .Select(u => new { OwnerId = u.OwnerId ?? u.Id, u.Role })
            .FirstOrDefaultAsync(cancellationToken);

        if (caller is null)
            return Result.Unauthorized();

        // Deletes are irreversible from the UI — restrict to Owner/Admin so a technician
        // can't wipe out company records unsupervised.
        if (caller.Role is not (UserRole.Owner or UserRole.Admin))
            return Result.Forbidden("Apenas o proprietário ou administradores podem apagar intervenções.");

        var ownerId = caller.OwnerId;

        var intervention = await db.Interventions
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        if (intervention is null)
            return Result.NotFound();

        if (intervention.UserId != ownerId)
            return Result.Forbidden();

        intervention.IsDeleted = true;
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
