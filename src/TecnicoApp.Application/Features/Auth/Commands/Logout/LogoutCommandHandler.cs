using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;

namespace TecnicoApp.Application.Features.Auth.Commands.Logout;

public sealed class LogoutCommandHandler(IAppDbContext db, ITokenService tokenService) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        var incomingHash = tokenService.HashRefreshToken(command.RefreshToken);
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.RefreshTokenHash == incomingHash, cancellationToken);

        if (user is not null)
        {
            user.RefreshTokenHash = null;
            user.RefreshTokenExpiresAt = null;
            user.ModifiedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
