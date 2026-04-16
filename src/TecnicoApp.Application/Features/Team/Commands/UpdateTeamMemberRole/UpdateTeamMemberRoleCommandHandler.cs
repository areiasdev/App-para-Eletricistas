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
