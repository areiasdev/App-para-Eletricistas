using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;

namespace TecnicoApp.Application.Features.Auth.Commands.ResetPassword;

public sealed class ResetPasswordCommandHandler(IAppDbContext db)
    : IRequestHandler<ResetPasswordCommand, Result>
{
    public async Task<Result> Handle(
        ResetPasswordCommand command,
        CancellationToken cancellationToken)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == command.Email.ToLowerInvariant(), cancellationToken);

        if (user is null
            || user.PasswordResetTokenHash is null
            || user.PasswordResetTokenExpiresAt is null
            || user.PasswordResetTokenExpiresAt < DateTime.UtcNow)
            return Result.Invalid(new ValidationError("Token inválido ou expirado."));

        if (!BCrypt.Net.BCrypt.Verify(command.Token, user.PasswordResetTokenHash))
            return Result.Invalid(new ValidationError("Token inválido ou expirado."));

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(command.NewPassword);
        user.PasswordResetTokenHash = null;
        user.PasswordResetTokenExpiresAt = null;
        // Invalidate any active refresh tokens for security
        user.RefreshToken = null;
        user.RefreshTokenExpiresAt = null;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
