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

        // A4 — Only Owner or Admin can invite members
        if (callingUser.Role != UserRole.Owner && callingUser.Role != UserRole.Admin)
            return Result.Forbidden();

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

        // B1 — Self-invite guard
        if (request.Email.Equals(callingUser.Email, StringComparison.OrdinalIgnoreCase))
            return Result.Error("Não podes convidar-te a ti próprio.");

        // M3 — Normalize email to avoid case-sensitive duplicates
        var normalizedEmail = request.Email.ToLowerInvariant();

        // C3 — Check TeamMembers table directly (not User.OwnerId) to detect existing/pending invites
        var existingMember = await db.TeamMembers
            .FirstOrDefaultAsync(
                t => t.OwnerId == ownerId && t.InviteEmail == normalizedEmail,
                cancellationToken);

        if (existingMember is not null)
            return Result.Error("Este email já foi convidado para a sua equipa.");

        // Look up if a user with this email already exists
        var existingUser = await db.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (existingUser is not null)
        {
            // M1 — Circular reference guard: reject if the invited user is already an owner of other users
            var isAlreadyAnOwner = await db.Users.AnyAsync(u => u.OwnerId == existingUser.Id, cancellationToken);
            if (isAlreadyAnOwner)
                return Result.Error("Este utilizador já é proprietário de uma equipa e não pode ser convidado como membro.");

            // Check that this user doesn't already belong to another team via TeamMembers
            var belongsToAnotherTeam = await db.TeamMembers
                .AnyAsync(t => t.MemberId == existingUser.Id && t.OwnerId != ownerId, cancellationToken);

            if (belongsToAnotherTeam)
                return Result.Error("Este utilizador já pertence a outra equipa.");
        }

        User memberUser;

        if (existingUser is not null)
        {
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
                Email = normalizedEmail,
                FullName = normalizedEmail, // placeholder until they accept
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
                OwnerId = ownerId,
                Role = request.Role,
                Plan = Plan.Free
            };
            db.Users.Add(memberUser);
        }

        // M5 — Generate invite token
        var rawToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');
        var tokenHash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(rawToken)));

        var teamMember = new TeamMember
        {
            OwnerId = ownerId,
            MemberId = memberUser.Id,
            Role = request.Role,
            InviteEmail = normalizedEmail,
            InviteTokenHash = tokenHash,
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
            teamMember.CreatedAt,
            rawToken));
    }
}
