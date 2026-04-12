using Ardalis.Result;
using MediatR;
using TecnicoApp.Application.Features.Interventions.DTOs;

namespace TecnicoApp.Application.Features.Interventions.Commands.CreateIntervention;

public record CreateInterventionCommand(
    string Title,
    string? Description,
    Guid ClientId,
    DateTime? ScheduledAt,
    Guid? QuoteId,
    IReadOnlyList<Guid> EquipmentIds
) : IRequest<Result<InterventionDto>>;
