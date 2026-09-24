namespace TecnicoApp.Application.Features.Equipment.DTOs;

public record EquipmentDto(
    Guid Id,
    string Type,
    string? Brand,
    string? Model,
    string? SerialNumber,
    DateTime? InstalledAt,
    DateTime? NextMaintenance,
    string? Notes,
    IReadOnlyList<string> Photos,
    Guid ClientId,
    string ClientName,
    DateTime CreatedAt,
    int? MaintenanceIntervalMonths = null
);

public record EquipmentListItemDto(
    Guid Id,
    string Type,
    string? Brand,
    string? Model,
    string? SerialNumber,
    DateTime? NextMaintenance,
    Guid ClientId,
    string ClientName,
    DateTime CreatedAt
);
