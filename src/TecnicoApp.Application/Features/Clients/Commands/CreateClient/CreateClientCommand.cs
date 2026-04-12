using Ardalis.Result;
using MediatR;
using TecnicoApp.Application.Features.Clients.DTOs;

namespace TecnicoApp.Application.Features.Clients.Commands.CreateClient;

public record CreateClientCommand(
    string Name,
    string? Nif,
    string? Email,
    string? Phone,
    string? Notes,
    CreateAddressCommand? Address
) : IRequest<Result<ClientDto>>;

public record CreateAddressCommand(
    string Street,
    string City,
    string PostalCode,
    string Country = "Portugal"
);
