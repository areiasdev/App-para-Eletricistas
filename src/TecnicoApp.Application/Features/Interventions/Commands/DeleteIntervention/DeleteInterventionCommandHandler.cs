using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;

namespace TecnicoApp.Application.Features.Interventions.Commands.DeleteIntervention;

public class DeleteInterventionCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<DeleteInterventionCommand, Result>
{
    public async Task<Result> Handle(
        DeleteInterventionCommand request, CancellationToken cancellationToken)
    {
        // Resolve ownerId: team members share their owner's interventions
        var ownerId = await db.Users.AsNoTracking()
            .Where(u => u.Id == currentUser.UserId)
            .Select(u => u.OwnerId ?? u.Id)
            .FirstOrDefaultAsync(cancellationToken);

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
