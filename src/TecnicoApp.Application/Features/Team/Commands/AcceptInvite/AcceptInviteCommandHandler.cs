using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;

namespace TecnicoApp.Application.Features.Team.Commands.AcceptInvite;

public class AcceptInviteCommandHandler(IAppDbContext db)
    : IRequestHandler<AcceptInviteCommand, Result>
{
    public async Task<Result> Handle(AcceptInviteCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(request.Token)));

        var teamMember = await db.TeamMembers
            .Include(t => t.Member)
            .FirstOrDefaultAsync(t => t.InviteTokenHash == tokenHash, cancellationToken);

        if (teamMember is null)
            return Result.NotFound("Convite não encontrado ou já utilizado.");

        if (teamMember.IsAccepted)
            return Result.Error("Este convite já foi aceite.");

        if (teamMember.InviteTokenExpiresAt is null || teamMember.InviteTokenExpiresAt < DateTime.UtcNow)
            return Result.Error("Este convite expirou. Pede ao proprietário para enviar um novo.");

        // Activate the member: set real name + password, mark accepted
        teamMember.Member.FullName = request.FullName;
        teamMember.Member.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        teamMember.Member.ModifiedAt = DateTime.UtcNow;

        teamMember.IsAccepted = true;
        teamMember.AcceptedAt = DateTime.UtcNow;
        teamMember.InviteTokenHash = null; // consume token
        teamMember.ModifiedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
