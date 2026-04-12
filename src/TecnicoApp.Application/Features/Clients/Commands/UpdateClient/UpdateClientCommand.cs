using Ardalis.Result;
using MediatR;
using TecnicoApp.Application.Features.Clients.Commands.CreateClient;
using TecnicoApp.Application.Features.Clients.DTOs;

namespace TecnicoApp.Application.Features.Clients.Commands.UpdateClient;

public record UpdateClientCommand(
    Guid ClientId,
    string Name,
    string? Nif,
    string? Email,
    string? Phone,
    string? Notes,
    CreateAddressCommand? Address
) : IRequest<Result<ClientDto>>;
