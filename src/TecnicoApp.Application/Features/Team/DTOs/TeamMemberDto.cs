using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Team.DTOs;

public record TeamMemberDto(
    Guid Id,
    Guid MemberId,
    string FullName,
    string Email,
    UserRole Role,
    bool IsAccepted,
    DateTime CreatedAt
);
