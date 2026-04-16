using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Team.DTOs;

namespace TecnicoApp.Application.Features.Team.Queries.GetTeam;

public class GetTeamQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetTeamQuery, Result<IReadOnlyList<TeamMemberDto>>>
{
    public async Task<Result<IReadOnlyList<TeamMemberDto>>> Handle(
        GetTeamQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        // Resolve ownerId: if this user is a member, use their OwnerId; otherwise use their own Id
        var ownerId = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.OwnerId ?? u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var members = await db.TeamMembers
            .AsNoTracking()
            .Include(t => t.Member)
            .Where(t => t.OwnerId == ownerId)
            .OrderBy(t => t.CreatedAt)
            .Select(t => new TeamMemberDto(
                t.Id,
                t.MemberId,
                t.Member.FullName,
                t.InviteEmail,
                t.Role,
                t.IsAccepted,
                t.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<TeamMemberDto>>(members);
    }
}
