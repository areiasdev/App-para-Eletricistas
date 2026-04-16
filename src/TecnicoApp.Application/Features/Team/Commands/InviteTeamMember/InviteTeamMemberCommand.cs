using Ardalis.Result;
using MediatR;
using TecnicoApp.Application.Features.Team.DTOs;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Team.Commands.InviteTeamMember;

public record InviteTeamMemberCommand(string Email, UserRole Role) : IRequest<Result<TeamMemberDto>>;
