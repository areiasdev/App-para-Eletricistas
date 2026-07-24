namespace TecnicoApp.Application.Features.Auth.DTOs;

/// <summary>
/// Internal DTO returned by command handlers — contains the refresh token so the
/// controller can set an httpOnly cookie before building the public response.
/// Never serialised and sent directly to the client.
/// </summary>
public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    UserDto User
);

/// <summary>Public response body — no refresh token exposed to JavaScript.</summary>
public record AuthPublicResponseDto(
    string AccessToken,
    string CsrfToken,
    UserDto User
);

public record UserDto(
    Guid Id,
    string FullName,
    string Email
);
