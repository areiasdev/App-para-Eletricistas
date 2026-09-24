using TecnicoApp.Domain.Entities;

namespace TecnicoApp.Application.Features.Interventions.DTOs;

public static class InterventionMappings
{
    /// <summary>Requires Equipment to be loaded. Names are passed in because they come from
    /// different places depending on the caller (loaded navigation, separate lookup, or unknown).</summary>
    public static InterventionDto ToDto(
        this Intervention i,
        string clientName,
        string? quoteNumber = null,
        string? assignedToName = null,
        Invoice? invoice = null) =>
        new(
            i.Id,
            i.Title,
            i.Description,
            i.Status,
            i.ScheduledAt,
            i.CompletedAt,
            i.TechnicianNotes,
            i.Photos,
            i.Materials,
            i.ClientId,
            clientName,
            i.QuoteId,
            quoteNumber,
            i.AssignedToUserId,
            assignedToName,
            i.Equipment.Select(e => new InterventionEquipmentDto(e.Id, e.Type, e.Brand, e.Model)).ToList(),
            i.CreatedAt,
            i.LaborHours,
            i.ClientSignatureUrl,
            i.SignedByName,
            i.SignedAt,
            invoice?.Id,
            invoice?.Number);
}
