using TecnicoApp.Domain.ValueObjects;

namespace TecnicoApp.Domain.Entities;

public class Client : BaseEntity
{
    public required string Name { get; set; }
    public string? Nif { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public Address? Address { get; set; }
    public string? Notes { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string? PortalTokenHash { get; set; }
    public DateTime? PortalTokenExpiresAt { get; set; }
    public int PortalTokenVersion { get; set; }

    public ICollection<Equipment> Equipment { get; set; } = [];
    public ICollection<Quote> Quotes { get; set; } = [];
    public ICollection<Intervention> Interventions { get; set; } = [];
}
