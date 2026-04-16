using Ardalis.Result;
using MediatR;
using TecnicoApp.Application.Features.Equipment.DTOs;

namespace TecnicoApp.Application.Features.Equipment.Commands.UpdateEquipment;

public record UpdateEquipmentCommand(
    Guid Id,
    string Type,
    string? Brand,
    string? Model,
    string? SerialNumber,
    DateTime? InstalledAt,
    DateTime? NextMaintenance,
    string? Notes,
    IReadOnlyList<string>? Photos
) : IRequest<Result<EquipmentDto>>;
