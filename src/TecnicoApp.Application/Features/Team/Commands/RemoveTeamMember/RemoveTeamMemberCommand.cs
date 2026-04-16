using Ardalis.Result;
using MediatR;

namespace TecnicoApp.Application.Features.Team.Commands.RemoveTeamMember;

public record RemoveTeamMemberCommand(Guid TeamMemberId) : IRequest<Result>;
