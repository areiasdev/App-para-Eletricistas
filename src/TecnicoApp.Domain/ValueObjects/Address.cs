namespace TecnicoApp.Domain.ValueObjects;

public record Address(
    string Street,
    string City,
    string PostalCode,
    string Country = "Portugal"
);
