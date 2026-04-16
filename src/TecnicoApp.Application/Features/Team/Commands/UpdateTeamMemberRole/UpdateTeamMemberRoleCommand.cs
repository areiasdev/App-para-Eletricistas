using Ardalis.Result;
using MediatR;
using TecnicoApp.Application.Features.Team.DTOs;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Team.Commands.UpdateTeamMemberRole;

public record UpdateTeamMemberRoleCommand(Guid TeamMemberId, UserRole Role) : IRequest<Result<TeamMemberDto>>;
