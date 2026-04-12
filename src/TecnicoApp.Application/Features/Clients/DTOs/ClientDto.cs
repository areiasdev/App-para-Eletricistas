namespace TecnicoApp.Application.Features.Clients.DTOs;

public record ClientDto(
    Guid Id,
    string Name,
    string? Nif,
    string? Email,
    string? Phone,
    AddressDto? Address,
    string? Notes,
    DateTime CreatedAt
);

public record ClientListItemDto(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    int QuoteCount,
    DateTime CreatedAt
);

public record AddressDto(
    string Street,
    string City,
    string PostalCode,
    string Country
);
