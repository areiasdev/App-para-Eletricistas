namespace TecnicoApp.Application.Features.Equipment.DTOs;

public static class EquipmentMappings
{
    public static EquipmentDto ToDto(this Domain.Entities.Equipment e, string clientName) =>
        new(
            e.Id, e.Type, e.Brand, e.Model, e.SerialNumber,
            e.InstalledAt, e.NextMaintenance, e.Notes, e.Photos,
            e.ClientId, clientName, e.CreatedAt,
            e.MaintenanceIntervalMonths);
}
