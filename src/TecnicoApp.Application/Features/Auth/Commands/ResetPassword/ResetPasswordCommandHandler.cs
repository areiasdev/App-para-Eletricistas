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

        // Always run BCrypt.Verify to prevent timing attacks that reveal
        // whether an email address has an active reset token.
        var dummyHash = "$2a$11$wBm59VnNtLNGAp5r3Q7rZuRrF1q8VVpYQ3VYY6g.7A5R3R3R3R3RO";
        var storedHash = user?.PasswordResetTokenHash ?? dummyHash;
        var tokenValid = BCrypt.Net.BCrypt.Verify(command.Token, storedHash);

        if (user is null
            || user.PasswordResetTokenHash is null
            || user.PasswordResetTokenExpiresAt is null
            || user.PasswordResetTokenExpiresAt < DateTime.UtcNow
            || !tokenValid)
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
