using Ardalis.Result;
using MediatR;
using TecnicoApp.Application.Features.Interventions.DTOs;

namespace TecnicoApp.Application.Features.Interventions.Commands.CreateIntervention;

public record InterventionMaterialRequest(string Name, decimal Quantity, decimal UnitCost);

public record CreateInterventionCommand(
    string Title,
    string? Description,
    Guid ClientId,
    DateTime? ScheduledAt,
    Guid? QuoteId,
    IReadOnlyList<Guid> EquipmentIds,
    IReadOnlyList<string>? Photos,
    IReadOnlyList<InterventionMaterialRequest>? Materials,
    Guid? AssignedToUserId
) : IRequest<Result<InterventionDto>>;
