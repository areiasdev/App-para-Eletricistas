using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Domain.Entities;

public class TeamMember : BaseEntity
{
    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;
    public Guid MemberId { get; set; }
    public User Member { get; set; } = null!;
    public UserRole Role { get; set; } = UserRole.Technician;
    public required string InviteEmail { get; set; }
    public string? InviteTokenHash { get; set; }
    public DateTime? InviteTokenExpiresAt { get; set; }
    public bool IsAccepted { get; set; }
    public DateTime? AcceptedAt { get; set; }
}
