using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
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

        // Always return success to avoid user enumeration
        if (user is null)
            return Result.Success();

        var token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        user.PasswordResetTokenHash = BCrypt.Net.BCrypt.HashPassword(token);
        user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);

        await db.SaveChangesAsync(cancellationToken);

        var resetLink = $"{appSettings.BaseUrl}/redefinir-password?token={token}&email={Uri.EscapeDataString(user.Email)}";
        var safeFullName = System.Net.WebUtility.HtmlEncode(user.FullName);

        await emailService.SendAsync(new EmailMessage(
            To: user.Email,
            ToName: user.FullName,
            Subject: "Redefinir a tua password — TécnicoApp",
            HtmlBody: $"""
                <p>Olá {safeFullName},</p>
                <p>Recebemos um pedido para redefinir a password da tua conta.</p>
                <p>
                  <a href="{resetLink}"
                     style="background:#f59e0b;color:#17171a;padding:10px 20px;border-radius:6px;text-decoration:none;font-weight:600;">
                    Redefinir password
                  </a>
                </p>
                <p>Este link expira em 1 hora. Se não fizeste este pedido, ignora este email.</p>
                <p>— Equipa TécnicoApp</p>
                """
        ), cancellationToken);

        return Result.Success();
    }
}
