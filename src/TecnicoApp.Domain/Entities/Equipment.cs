namespace TecnicoApp.Domain.Entities;

public class Equipment : BaseEntity
{
    public required string Type { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime? InstalledAt { get; set; }
    public DateTime? NextMaintenance { get; set; }
    public string? Notes { get; set; }
    public List<string> Photos { get; set; } = [];

    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public ICollection<Intervention> Interventions { get; set; } = [];
}
