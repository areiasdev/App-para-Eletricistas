using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Team.DTOs;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Team.Commands.InviteTeamMember;

public class InviteTeamMemberCommandHandler(
    IAppDbContext db,
    ICurrentUserService currentUser,
    IPlanGateService planGate)
    : IRequestHandler<InviteTeamMemberCommand, Result<TeamMemberDto>>
{
    public async Task<Result<TeamMemberDto>> Handle(
        InviteTeamMemberCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        // Load the calling user to determine owner context
        var callingUser = await db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (callingUser is null)
            return Result.Unauthorized();

        // Only owners (or admins acting as owner) can invite — use their own account as owner
        var ownerId = callingUser.OwnerId ?? callingUser.Id;

        var ownerUser = callingUser.OwnerId.HasValue
            ? await db.Users.FirstOrDefaultAsync(u => u.Id == ownerId, cancellationToken)
            : callingUser;

        if (ownerUser is null)
            return Result.Error("Conta de proprietário não encontrada.");

        // Check plan allows team
        if (!planGate.CanUseTeam(ownerUser.Plan))
            return Result.Error("O seu plano não permite gestão de equipa. Actualize para o plano Team ou Enterprise.");

        // Check max team members not exceeded
        var maxMembers = planGate.MaxTeamMembers(ownerUser.Plan);
        if (maxMembers >= 0)
        {
            var currentCount = await db.TeamMembers
                .CountAsync(t => t.OwnerId == ownerId, cancellationToken);

            if (currentCount >= maxMembers)
                return Result.Error($"Limite de {maxMembers} membros de equipa atingido para o seu plano.");
        }

        // Check if this email is already a member
        var existingMember = await db.TeamMembers
            .FirstOrDefaultAsync(t => t.OwnerId == ownerId && t.InviteEmail == request.Email, cancellationToken);

        if (existingMember is not null)
            return Result.Error("Este email já foi convidado para a sua equipa.");

        // Look up if a user with this email already exists
        var existingUser = await db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        User memberUser;

        if (existingUser is not null)
        {
            // Check if they already belong to this owner
            if (existingUser.OwnerId == ownerId)
                return Result.Error("Este utilizador já pertence à sua equipa.");

            // Check if they already belong to another owner
            if (existingUser.OwnerId.HasValue && existingUser.OwnerId != ownerId)
                return Result.Error("Este utilizador já pertence a outra equipa.");

            memberUser = existingUser;
            memberUser.OwnerId = ownerId;
            memberUser.Role = request.Role;
        }
        else
        {
            // Create stub user — they'll reset their password via invite link later
            var tempPassword = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            memberUser = new User
            {
                Email = request.Email,
                FullName = request.Email, // placeholder until they accept
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
                OwnerId = ownerId,
                Role = request.Role,
                Plan = Plan.Free
            };
            db.Users.Add(memberUser);
        }

        var teamMember = new TeamMember
        {
            OwnerId = ownerId,
            MemberId = memberUser.Id,
            Role = request.Role,
            InviteEmail = request.Email,
            IsAccepted = false
        };

        db.TeamMembers.Add(teamMember);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new TeamMemberDto(
            teamMember.Id,
            memberUser.Id,
            memberUser.FullName,
            teamMember.InviteEmail,
            teamMember.Role,
            teamMember.IsAccepted,
            teamMember.CreatedAt));
    }
}
