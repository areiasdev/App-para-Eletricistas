using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Interventions.DTOs;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.Domain.ValueObjects;

namespace TecnicoApp.Application.Features.Interventions.Commands.CreateIntervention;

public class CreateInterventionCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CreateInterventionCommand, Result<InterventionDto>>
{
    public async Task<Result<InterventionDto>> Handle(
        CreateInterventionCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        // Resolve ownerId: team members see their owner's data
        var ownerId = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.OwnerId ?? u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var ownerExists = await db.Users.AsNoTracking()
            .AnyAsync(u => u.Id == ownerId, cancellationToken);

        if (!ownerExists)
            return Result.Unauthorized();

        // C2/H6 — AssignedToUserId must belong to owner's team AND have accepted the invite
        if (request.AssignedToUserId.HasValue && request.AssignedToUserId.Value != ownerId)
        {
            var isActiveTeamMember = await db.TeamMembers.AsNoTracking()
                .AnyAsync(t => t.MemberId == request.AssignedToUserId.Value
                            && t.OwnerId == ownerId
                            && t.IsAccepted, cancellationToken);

            if (!isActiveTeamMember)
                return Result.Error("O utilizador atribuído não pertence à sua equipa ou ainda não aceitou o convite.");
        }

        var client = await db.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ClientId, cancellationToken);

        if (client is null)
            return Result.NotFound("Cliente não encontrado.");

        if (client.UserId != ownerId)
            return Result.Forbidden();

        // Validate quote belongs to the same owner and client
        if (request.QuoteId.HasValue)
        {
            var quote = await db.Quotes
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == request.QuoteId.Value, cancellationToken);

            if (quote is null)
                return Result.NotFound("Orçamento não encontrado.");

            if (quote.UserId != ownerId)
                return Result.Forbidden();

            if (quote.ClientId != request.ClientId)
                return Result.Error("O orçamento não pertence ao cliente selecionado.");
        }

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

        var materials = request.Materials?
            .Select(m => new InterventionMaterial(m.Name, m.Quantity, m.UnitCost))
            .ToList() ?? [];

        var intervention = new Intervention
        {
            Title = request.Title,
            Description = request.Description,
            ClientId = request.ClientId,
            UserId = ownerId,          // scope to owner's account
            AssignedToUserId = userId != ownerId ? userId : request.AssignedToUserId,
            ScheduledAt = request.ScheduledAt,
            QuoteId = request.QuoteId,
            Equipment = equipment,
            Photos = request.Photos?.ToList() ?? [],
            Materials = materials,
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
            intervention.Photos,
            intervention.Materials,
            intervention.ClientId,
            client.Name,
            intervention.QuoteId,
            null,
            intervention.AssignedToUserId,
            null,
            equipment.Select(e => new InterventionEquipmentDto(e.Id, e.Type, e.Brand, e.Model)).ToList(),
            intervention.CreatedAt
        ));
    }
}
