using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Interventions.DTOs;
using TecnicoApp.Domain.Entities;

namespace TecnicoApp.Application.Features.Interventions.Commands.CreateIntervention;

public class CreateInterventionCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CreateInterventionCommand, Result<InterventionDto>>
{
    public async Task<Result<InterventionDto>> Handle(
        CreateInterventionCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var client = await db.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ClientId, cancellationToken);

        if (client is null)
            return Result.NotFound("Cliente não encontrado.");

        if (client.UserId != userId)
            return Result.Forbidden();

        // Validate equipment belongs to this client
        var equipment = new List<Domain.Entities.Equipment>();
        if (request.EquipmentIds.Count > 0)
        {
            equipment = await db.Equipment
                .Where(e => request.EquipmentIds.Contains(e.Id) && e.ClientId == request.ClientId)
                .ToListAsync(cancellationToken);

            if (equipment.Count != request.EquipmentIds.Count)
                return Result.Error("Um ou mais equipamentos não pertencem ao cliente selecionado.");
        }

        var intervention = new Intervention
        {
            Title = request.Title,
            Description = request.Description,
            ClientId = request.ClientId,
            UserId = userId,
            ScheduledAt = request.ScheduledAt,
            QuoteId = request.QuoteId,
            Equipment = equipment,
        };

        db.Interventions.Add(intervention);
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
            client.Name,
            intervention.QuoteId,
            null,
            equipment.Select(e => new InterventionEquipmentDto(e.Id, e.Type, e.Brand, e.Model)).ToList(),
            intervention.CreatedAt
        ));
    }
}
