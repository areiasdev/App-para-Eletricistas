using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Interventions.DTOs;

namespace TecnicoApp.Application.Features.Interventions.Queries.GetInterventionById;

public class GetInterventionByIdQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetInterventionByIdQuery, Result<InterventionDto>>
{
    public async Task<Result<InterventionDto>> Handle(
        GetInterventionByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        // Resolve ownerId: team members see their owner's data
        var ownerId = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.OwnerId ?? u.Id)
            .FirstOrDefaultAsync(cancellationToken);

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

        return Result.Success(new InterventionDto(
            intervention.Id,
            intervention.Title,
            intervention.Description,
            intervention.Status,
            intervention.ScheduledAt,
            intervention.CompletedAt,
            intervention.TechnicianNotes,
            intervention.Photos,
            intervention.Materials,
            intervention.ClientId,
            intervention.Client.Name,
            intervention.QuoteId,
            intervention.Quote?.Number,
            intervention.AssignedToUserId,
            assignedToName,
            intervention.Equipment
                .Select(e => new InterventionEquipmentDto(e.Id, e.Type, e.Brand, e.Model))
                .ToList(),
            intervention.CreatedAt
        ));
    }
}
