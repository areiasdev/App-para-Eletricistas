using Ardalis.Result;
using MediatR;

namespace TecnicoApp.Application.Features.Clients.Commands.DeleteClient;

public record DeleteClientCommand(Guid ClientId) : IRequest<Result>;
