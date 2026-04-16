using Ardalis.Result;
using MediatR;
using TecnicoApp.Application.Features.Team.DTOs;

namespace TecnicoApp.Application.Features.Team.Queries.GetTeam;

public record GetTeamQuery() : IRequest<Result<IReadOnlyList<TeamMemberDto>>>;
