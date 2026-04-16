using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;

namespace TecnicoApp.Application.Features.Team.Commands.RemoveTeamMember;

public class RemoveTeamMemberCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<RemoveTeamMemberCommand, Result>
{
    public async Task<Result> Handle(
        RemoveTeamMemberCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var ownerId = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.OwnerId ?? u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var teamMember = await db.TeamMembers
            .Include(t => t.Member)
            .FirstOrDefaultAsync(t => t.Id == request.TeamMemberId, cancellationToken);

        if (teamMember is null)
            return Result.NotFound("Membro não encontrado.");

        if (teamMember.OwnerId != ownerId)
            return Result.Forbidden();

        // Clear OwnerId on the member user so they become independent again
        teamMember.Member.OwnerId = null;
        teamMember.Member.ModifiedAt = DateTime.UtcNow;

        // Soft delete the team member record
        teamMember.IsDeleted = true;
        teamMember.ModifiedAt = DateTime.UtcNow;
        teamMember.ModifiedBy = currentUser.Email;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
