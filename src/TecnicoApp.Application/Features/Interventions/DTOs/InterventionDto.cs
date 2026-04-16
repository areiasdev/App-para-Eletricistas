using TecnicoApp.Domain.Enums;
using TecnicoApp.Domain.ValueObjects;

namespace TecnicoApp.Application.Features.Interventions.DTOs;

public record InterventionEquipmentDto(Guid Id, string Type, string? Brand, string? Model);

public record InterventionDto(
    Guid Id,
    string Title,
    string? Description,
    InterventionStatus Status,
    DateTime? ScheduledAt,
    DateTime? CompletedAt,
    string? TechnicianNotes,
    IReadOnlyList<string> Photos,
    IReadOnlyList<InterventionMaterial> Materials,
    Guid ClientId,
    string ClientName,
    Guid? QuoteId,
    string? QuoteNumber,
    Guid? AssignedToUserId,
    string? AssignedToName,
    IReadOnlyList<InterventionEquipmentDto> Equipment,
    DateTime CreatedAt
);

public record InterventionListItemDto(
    Guid Id,
    string Title,
    InterventionStatus Status,
    DateTime? ScheduledAt,
    DateTime? CompletedAt,
    Guid ClientId,
    string ClientName,
    int EquipmentCount,
    DateTime CreatedAt
);
