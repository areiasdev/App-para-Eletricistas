using Ardalis.Result;
using MediatR;
using TecnicoApp.Application.Features.Clients.DTOs;

namespace TecnicoApp.Application.Features.Clients.Queries.GetClientById;

public record GetClientByIdQuery(Guid ClientId) : IRequest<Result<ClientDto>>;
