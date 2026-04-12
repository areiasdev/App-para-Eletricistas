using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Interventions.Commands.UpdateInterventionStatus;

public class UpdateInterventionStatusCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpdateInterventionStatusCommand, Result>
{
    public async Task<Result> Handle(
        UpdateInterventionStatusCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var intervention = await db.Interventions
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        if (intervention is null)
            return Result.NotFound();

        if (intervention.UserId != userId)
            return Result.Forbidden();

        var valid = (intervention.Status, request.Status) switch
        {
            (InterventionStatus.Scheduled, InterventionStatus.InProgress) => true,
            (InterventionStatus.InProgress, InterventionStatus.Completed) => true,
            (InterventionStatus.InProgress, InterventionStatus.Scheduled) => true, // allow rollback
            _ => false
        };

        if (!valid)
            return Result.Error($"Transição inválida: {intervention.Status} → {request.Status}.");

        intervention.Status = request.Status;
        if (request.Status == InterventionStatus.Completed)
            intervention.CompletedAt = DateTime.UtcNow;
        else if (request.Status != InterventionStatus.Completed)
            intervention.CompletedAt = null;

        intervention.ModifiedBy = currentUser.Email;
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
