using Ardalis.Result;
using MediatR;

namespace TecnicoApp.Application.Features.Team.Commands.AcceptInvite;

public record AcceptInviteCommand(string Token, string FullName, string NewPassword) : IRequest<Result>;
