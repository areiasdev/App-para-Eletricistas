using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Team.DTOs;

namespace TecnicoApp.Application.Features.Team.Commands.UpdateTeamMemberRole;

public class UpdateTeamMemberRoleCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpdateTeamMemberRoleCommand, Result<TeamMemberDto>>
{
    public async Task<Result<TeamMemberDto>> Handle(
        UpdateTeamMemberRoleCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var callingUser = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (callingUser is null)
            return Result.Unauthorized();

        var ownerId = callingUser.OwnerId ?? callingUser.Id;

        var teamMember = await db.TeamMembers
            .Include(t => t.Member)
            .FirstOrDefaultAsync(t => t.Id == request.TeamMemberId, cancellationToken);

        if (teamMember is null)
            return Result.NotFound("Membro não encontrado.");

        if (teamMember.OwnerId != ownerId)
            return Result.Forbidden();

        // Prevent self-role-change
        if (teamMember.MemberId == userId)
            return Result.Forbidden("Não podes alterar o teu próprio papel.");

        // Note: the Owner role can never reach here — the validator already rejects it
        // outright, since ownership transfer isn't supported through this operation.
        teamMember.Role = request.Role;
        teamMember.Member.Role = request.Role;
        teamMember.ModifiedAt = DateTime.UtcNow;
        teamMember.ModifiedBy = currentUser.Email;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new TeamMemberDto(
            teamMember.Id,
            teamMember.MemberId,
            teamMember.Member.FullName,
            teamMember.InviteEmail,
            teamMember.Role,
            teamMember.IsAccepted,
            teamMember.CreatedAt));
    }
}
