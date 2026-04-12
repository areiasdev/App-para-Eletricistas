using Ardalis.Result;
using MediatR;
using TecnicoApp.Application.Features.Interventions.DTOs;

namespace TecnicoApp.Application.Features.Interventions.Commands.UpdateIntervention;

public record UpdateInterventionCommand(
    Guid Id,
    string Title,
    string? Description,
    DateTime? ScheduledAt,
    string? TechnicianNotes,
    Guid? QuoteId,
    IReadOnlyList<Guid> EquipmentIds
) : IRequest<Result<InterventionDto>>;
