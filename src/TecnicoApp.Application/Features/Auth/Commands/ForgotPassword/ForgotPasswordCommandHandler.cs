using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Email;
using TecnicoApp.Application.Common.Interfaces;

namespace TecnicoApp.Application.Features.Auth.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandHandler(
    IAppDbContext db,
    IEmailService emailService,
    IAppSettings appSettings)
    : IRequestHandler<ForgotPasswordCommand, Result>
{
    public async Task<Result> Handle(
        ForgotPasswordCommand command,
        CancellationToken cancellationToken)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == command.Email.ToLowerInvariant(), cancellationToken);

        var token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

        // Always run BCrypt — whether or not the user exists — so response timing doesn't
        // reveal which emails are registered (same reasoning as ResetPasswordCommandHandler).
        var tokenHash = BCrypt.Net.BCrypt.HashPassword(token);

        // Always return success to avoid user enumeration
        if (user is null)
            return Result.Success();

        user.PasswordResetTokenHash = tokenHash;
        user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);

        await db.SaveChangesAsync(cancellationToken);

        var resetLink = $"{appSettings.BaseUrl}/redefinir-password?token={token}&email={Uri.EscapeDataString(user.Email)}";
        var branding = new EmailBranding(appSettings.ProductName);
        var body = $"""
            <p style="margin:0 0 16px;">Olá {EmailLayout.Encode(user.FullName)},</p>
            <p style="margin:0 0 24px;">Recebemos um pedido para redefinir a password da tua conta.</p>
            {EmailLayout.Button(branding, resetLink, "Redefinir password")}
            <p style="margin:0;color:#6b7280;font-size:13px;">Este link expira em 1 hora. Se não fizeste este pedido, ignora este email.</p>
            """;

        await emailService.SendAsync(new EmailMessage(
            To: user.Email,
            ToName: user.FullName,
            Subject: $"Redefinir a tua password — {appSettings.ProductName}",
            HtmlBody: EmailLayout.Render(branding, body, appSettings.ProductName)
        ), cancellationToken);

        return Result.Success();
    }
}
