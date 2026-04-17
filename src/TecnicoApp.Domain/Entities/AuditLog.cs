namespace TecnicoApp.Domain.Entities;

public class AuditLog
{
    public long Id { get; init; }
    public required string EntityType { get; init; }
    public required string EntityId { get; init; }
    public required string Action { get; init; }     // "Created" | "Updated" | "Deleted"
    public Guid? UserId { get; init; }
    public string? UserEmail { get; init; }
    public string? Changes { get; init; }            // JSON summary of modified properties
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
