using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Interventions.DTOs;
using TecnicoApp.Application.Common.Extensions;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Interventions.Queries.GetInterventionById;

public class GetInterventionByIdQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetInterventionByIdQuery, Result<InterventionDto>>
{
    public async Task<Result<InterventionDto>> Handle(
        GetInterventionByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        // Resolve ownerId: team members see their owner's data
        var ownerId = await db.ResolveOwnerIdAsync(userId, cancellationToken);

        var intervention = await db.Interventions
            .AsNoTracking()
            .Include(i => i.Client)
            .Include(i => i.Quote)
            .Include(i => i.Equipment)
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        if (intervention is null)
            return Result.NotFound();

        if (intervention.UserId != ownerId)
            return Result.Forbidden();

        // Resolve assigned-to name if present
        string? assignedToName = null;
        if (intervention.AssignedToUserId.HasValue)
        {
            assignedToName = await db.Users.AsNoTracking()
                .Where(u => u.Id == intervention.AssignedToUserId.Value)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var invoice = await db.Invoices.AsNoTracking()
            .Where(i => i.InterventionId == intervention.Id && i.Status != InvoiceStatus.Cancelled)
            .FirstOrDefaultAsync(cancellationToken);

        return Result.Success(intervention.ToDto(
            intervention.Client.Name, intervention.Quote?.Number, assignedToName, invoice));
    }
}
