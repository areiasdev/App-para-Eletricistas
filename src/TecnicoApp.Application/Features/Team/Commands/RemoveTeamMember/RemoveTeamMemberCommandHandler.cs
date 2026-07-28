using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Team.Commands.RemoveTeamMember;

public class RemoveTeamMemberCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<RemoveTeamMemberCommand, Result>
{
    public async Task<Result> Handle(
        RemoveTeamMemberCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var caller = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { OwnerId = u.OwnerId ?? u.Id, u.Role })
            .FirstOrDefaultAsync(cancellationToken);

        if (caller is null)
            return Result.Unauthorized();

        if (caller.Role is not (UserRole.Owner or UserRole.Admin))
            return Result.Forbidden("Apenas o proprietário ou administradores podem remover membros da equipa.");

        var ownerId = caller.OwnerId;

        var teamMember = await db.TeamMembers
            .Include(t => t.Member)
            .FirstOrDefaultAsync(t => t.Id == request.TeamMemberId, cancellationToken);

        if (teamMember is null)
            return Result.NotFound("Membro não encontrado.");

        if (teamMember.OwnerId != ownerId)
            return Result.Forbidden();

        var memberId = teamMember.MemberId;

        // M2: Re-assign interventions created by this member back to the owner
        var memberInterventions = await db.Interventions
            .Where(i => i.UserId == memberId)
            .ToListAsync(cancellationToken);
        foreach (var iv in memberInterventions)
            iv.UserId = ownerId;

        // M2: Clear AssignedToUserId on interventions pointing to this member
        var assignedInterventions = await db.Interventions
            .Where(i => i.AssignedToUserId == memberId)
            .ToListAsync(cancellationToken);
        foreach (var iv in assignedInterventions)
            iv.AssignedToUserId = null;

        // Clear OwnerId on the member user so they become independent again
        teamMember.Member.OwnerId = null;
        teamMember.Member.Role = Domain.Enums.UserRole.Owner;
        teamMember.Member.ModifiedAt = DateTime.UtcNow;

        // Soft delete the team member record
        teamMember.IsDeleted = true;
        teamMember.ModifiedAt = DateTime.UtcNow;
        teamMember.ModifiedBy = currentUser.Email;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
