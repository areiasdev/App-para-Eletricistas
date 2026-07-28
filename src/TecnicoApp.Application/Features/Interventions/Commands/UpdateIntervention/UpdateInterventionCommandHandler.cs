using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Interventions.DTOs;
using TecnicoApp.Domain.ValueObjects;

namespace TecnicoApp.Application.Features.Interventions.Commands.UpdateIntervention;

public class UpdateInterventionCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpdateInterventionCommand, Result<InterventionDto>>
{
    public async Task<Result<InterventionDto>> Handle(
        UpdateInterventionCommand request, CancellationToken cancellationToken)
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

        var intervention = await db.Interventions
            .Include(i => i.Client)
            .Include(i => i.Quote)
            .Include(i => i.Equipment)
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        if (intervention is null)
            return Result.NotFound();

        if (intervention.UserId != ownerId)
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

        // Validate quote ownership and client match when QuoteId changes
        if (request.QuoteId != intervention.QuoteId && request.QuoteId.HasValue)
        {
            var quote = await db.Quotes
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == request.QuoteId.Value, cancellationToken);

            if (quote is null)
                return Result.NotFound("Orçamento não encontrado.");

            if (quote.UserId != ownerId)
                return Result.Forbidden();

            if (quote.ClientId != intervention.ClientId)
                return Result.Error("O orçamento não pertence ao cliente selecionado.");
        }

        intervention.Title = request.Title;
        intervention.Description = request.Description;
        intervention.ScheduledAt = request.ScheduledAt;
        intervention.TechnicianNotes = request.TechnicianNotes;
        intervention.Photos = request.Photos?.ToList() ?? intervention.Photos;
        intervention.Materials = request.Materials?
            .Select(m => new InterventionMaterial(m.Name, m.Quantity, m.UnitCost))
            .ToList() ?? intervention.Materials;
        intervention.QuoteId = request.QuoteId;
        // Only the owner/admin can (re)assign work to someone else — a technician can only
        // ever assign to themselves. Matches CreateInterventionCommandHandler's rule.
        intervention.AssignedToUserId = userId != ownerId ? userId : request.AssignedToUserId;
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
            intervention.Photos,
            intervention.Materials,
            intervention.ClientId,
            intervention.Client.Name,
            intervention.QuoteId,
            intervention.Quote?.Number,
            intervention.AssignedToUserId,
            null,
            intervention.Equipment
                .Select(e => new InterventionEquipmentDto(e.Id, e.Type, e.Brand, e.Model))
                .ToList(),
            intervention.CreatedAt
        ));
    }
}
