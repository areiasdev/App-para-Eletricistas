using TecnicoApp.Domain.Enums;
using TecnicoApp.Domain.ValueObjects;

namespace TecnicoApp.Domain.Entities;

public class Intervention : BaseEntity
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public InterventionStatus Status { get; set; } = InterventionStatus.Scheduled;
    public DateTime? ScheduledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? TechnicianNotes { get; set; }
    public List<string> Photos { get; set; } = [];
    public List<InterventionMaterial> Materials { get; set; } = [];
    public string? ReportPdfUrl { get; set; }

    /// <summary>Hours worked on site, billed at the company's hourly rate when invoicing the job.</summary>
    public decimal? LaborHours { get; set; }

    // Client sign-off on the job sheet (folha de obra) — a PNG/JPEG data URI, same format as quotes.
    public string? ClientSignatureUrl { get; set; }
    public string? SignedByName { get; set; }
    public DateTime? SignedAt { get; set; }

    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid? QuoteId { get; set; }
    public Quote? Quote { get; set; }
    public Guid? AssignedToUserId { get; set; }

    public ICollection<Equipment> Equipment { get; set; } = [];

    /// <summary>
    /// Marks the job done and rolls each serviced equipment's next maintenance date forward by
    /// its interval (e.g. yearly boiler/AC/PT service) — so the maintenance alerts keep coming
    /// without anyone re-typing dates. Requires <see cref="Equipment"/> to be loaded.
    /// </summary>
    public void Complete(DateTime completedAt)
    {
        Status = InterventionStatus.Completed;
        CompletedAt = completedAt;

        foreach (var equipment in Equipment.Where(e => e.MaintenanceIntervalMonths is > 0))
            equipment.NextMaintenance = completedAt.Date.AddMonths(equipment.MaintenanceIntervalMonths!.Value);
    }
}
