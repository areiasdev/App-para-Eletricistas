namespace TecnicoApp.Application.Features.Auth.DTOs;

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    UserDto User
);

public record UserDto(
    Guid Id,
    string FullName,
    string Email,
    string Plan
);
