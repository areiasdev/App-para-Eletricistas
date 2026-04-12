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

        var intervention = await db.Interventions
            .AsNoTracking()
            .Include(i => i.Client)
            .Include(i => i.Quote)
            .Include(i => i.Equipment)
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        if (intervention is null)
            return Result.NotFound();

        if (intervention.UserId != userId)
            return Result.Forbidden();

        return Result.Success(new InterventionDto(
            intervention.Id,
            intervention.Title,
            intervention.Description,
            intervention.Status,
            intervention.ScheduledAt,
            intervention.CompletedAt,
            intervention.TechnicianNotes,
            intervention.ClientId,
            intervention.Client.Name,
            intervention.QuoteId,
            intervention.Quote?.Number,
            intervention.Equipment
                .Select(e => new InterventionEquipmentDto(e.Id, e.Type, e.Brand, e.Model))
                .ToList(),
            intervention.CreatedAt
        ));
    }
}
