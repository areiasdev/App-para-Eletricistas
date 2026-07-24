namespace TecnicoApp.Application.Features.Users.DTOs;

public record ProfileDto(
    Guid Id,
    string FullName,
    string Email,
    string? CompanyName,
    string? Nif,
    string? Phone,
    string? LogoUrl
);
