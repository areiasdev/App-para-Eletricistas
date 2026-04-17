using Ardalis.Result.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TecnicoApp.Application.Features.Auth.Commands.ForgotPassword;
using TecnicoApp.Application.Features.Auth.Commands.Login;
using TecnicoApp.Application.Features.Auth.Commands.Logout;
using TecnicoApp.Application.Features.Auth.Commands.RefreshToken;
using TecnicoApp.Application.Features.Auth.Commands.Register;
using TecnicoApp.Application.Features.Auth.Commands.ResetPassword;
using TecnicoApp.Application.Features.Auth.DTOs;

namespace TecnicoApp.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[EnableRateLimiting("auth")]
public class AuthController(IMediator mediator, IWebHostEnvironment env) : ControllerBase
{
    private const string RefreshTokenCookie = "refreshToken";

    private void SetRefreshTokenCookie(string token, DateTime expiresAt)
    {
        Response.Cookies.Append(RefreshTokenCookie, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !env.IsDevelopment(), // HTTPS only in production
            SameSite = env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None,
            Expires = expiresAt,
            Path = "/api/v1/auth",
        });
    }

    private void ClearRefreshTokenCookie() =>
        Response.Cookies.Delete(RefreshTokenCookie, new CookieOptions
        {
            HttpOnly = true,
            Secure = !env.IsDevelopment(),
            SameSite = env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None,
            Path = "/api/v1/auth",
        });

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthPublicResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterCommand command,
        CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        if (!result.IsSuccess)
            return ((Microsoft.AspNetCore.Mvc.Infrastructure.IConvertToActionResult)
                result.ToActionResult(this)).Convert();

        SetRefreshTokenCookie(result.Value.RefreshToken, result.Value.RefreshTokenExpiresAt);
        return CreatedAtAction(nameof(Register),
            new AuthPublicResponseDto(result.Value.AccessToken, result.Value.User));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthPublicResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginCommand command,
        CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        if (!result.IsSuccess)
            return ((Microsoft.AspNetCore.Mvc.Infrastructure.IConvertToActionResult)
                result.ToActionResult(this)).Convert();

        SetRefreshTokenCookie(result.Value.RefreshToken, result.Value.RefreshTokenExpiresAt);
        return Ok(new AuthPublicResponseDto(result.Value.AccessToken, result.Value.User));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthPublicResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookie];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized();

        var result = await mediator.Send(new RefreshTokenCommand(refreshToken), ct);
        if (!result.IsSuccess)
            return ((Microsoft.AspNetCore.Mvc.Infrastructure.IConvertToActionResult)
                result.ToActionResult(this)).Convert();

        SetRefreshTokenCookie(result.Value.RefreshToken, result.Value.RefreshTokenExpiresAt);
        return Ok(new AuthPublicResponseDto(result.Value.AccessToken, result.Value.User));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookie];
        if (!string.IsNullOrEmpty(refreshToken))
            await mediator.Send(new LogoutCommand(refreshToken), ct);

        ClearRefreshTokenCookie();
        return NoContent();
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordCommand command,
        CancellationToken ct)
    {
        await mediator.Send(command, ct);
        return NoContent(); // Always 204 to avoid user enumeration
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordCommand command,
        CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return result.IsSuccess ? NoContent() : result.ToActionResult(this);
    }
}
