using TecnicoApp.Domain.Enums;

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
    public string? ReportPdfUrl { get; set; }

    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid? QuoteId { get; set; }
    public Quote? Quote { get; set; }

    public ICollection<Equipment> Equipment { get; set; } = [];
}
