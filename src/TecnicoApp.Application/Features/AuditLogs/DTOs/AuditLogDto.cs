namespace TecnicoApp.Application.Features.AuditLogs.DTOs;

public record AuditLogDto(
    long Id,
    string EntityType,
    string EntityId,
    string Action,
    Guid? UserId,
    string? UserEmail,
    string? Changes,
    DateTime OccurredAt
);
