using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Interventions.DTOs;

namespace TecnicoApp.Application.Features.Interventions.Commands.UpdateIntervention;

public class UpdateInterventionCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpdateInterventionCommand, Result<InterventionDto>>
{
    public async Task<Result<InterventionDto>> Handle(
        UpdateInterventionCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var intervention = await db.Interventions
            .Include(i => i.Client)
            .Include(i => i.Quote)
            .Include(i => i.Equipment)
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        if (intervention is null)
            return Result.NotFound();

        if (intervention.UserId != userId)
            return Result.Forbidden();

        // Update equipment set
        if (request.EquipmentIds.Count > 0)
        {
            var newEquipment = await db.Equipment
                .Where(e => request.EquipmentIds.Contains(e.Id) && e.ClientId == intervention.ClientId)
                .ToListAsync(cancellationToken);

            if (newEquipment.Count != request.EquipmentIds.Count)
                return Result.Error("Um ou mais equipamentos não pertencem ao cliente selecionado.");

            intervention.Equipment.Clear();
            foreach (var e in newEquipment)
                intervention.Equipment.Add(e);
        }
        else
        {
            intervention.Equipment.Clear();
        }

        intervention.Title = request.Title;
        intervention.Description = request.Description;
        intervention.ScheduledAt = request.ScheduledAt;
        intervention.TechnicianNotes = request.TechnicianNotes;
        intervention.QuoteId = request.QuoteId;
        intervention.ModifiedBy = currentUser.Email;

        await db.SaveChangesAsync(cancellationToken);

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
