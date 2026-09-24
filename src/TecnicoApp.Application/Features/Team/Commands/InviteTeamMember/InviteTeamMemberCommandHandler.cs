using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecnicoApp.Application.Common.Email;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Team.DTOs;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Team.Commands.InviteTeamMember;

public class InviteTeamMemberCommandHandler(
    IAppDbContext db,
    ICurrentUserService currentUser,
    IEmailService emailService,
    IAppSettings appSettings,
    ILogger<InviteTeamMemberCommandHandler> logger)
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
            // Do NOT reassign OwnerId/Role here: this is an already-active account and
            // reassigning its tenant now (before the invite is accepted) would silently
            // hijack it out from under the current owner the instant this request commits.
            // The reassignment happens in AcceptInviteCommandHandler instead, gated on the
            // invitee proving control of the invite token.
            memberUser = existingUser;
        }
        else
        {
            // Create stub user — they'll reset their password via invite link later.
            // OwnerId/Role are intentionally left unset (defaults) until acceptance; the
            // account has a random, never-disclosed password so it can't be used before then.
            var tempPassword = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            memberUser = new User
            {
                Email = normalizedEmail,
                FullName = normalizedEmail, // placeholder until they accept
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword)
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
            InviteTokenExpiresAt = DateTime.UtcNow.AddDays(7),
            IsAccepted = false
        };

        db.TeamMembers.Add(teamMember);
        await db.SaveChangesAsync(cancellationToken);

        // Send invite email (best-effort — don't fail the invite if email fails)
        try
        {
            var inviteUrl = $"{appSettings.BaseUrl}/aceitar-convite?token={rawToken}";
            var branding = EmailBranding.ForCompany(ownerUser!);
            var body = $"""
                <h1 style="font-size:22px;font-weight:700;margin:0 0 8px;color:#1a1a1a;">Foste convidado para a equipa</h1>
                <p style="margin:0 0 8px;"><strong>{EmailLayout.Encode(ownerUser.FullName)}</strong> convidou-te para te juntares à equipa de <strong>{EmailLayout.Encode(branding.SenderName)}</strong>.</p>
                <p style="margin:0 0 24px;">Clica no botão abaixo para configurar a tua conta. O link é válido por 7 dias.</p>
                {EmailLayout.Button(branding, inviteUrl, "Aceitar convite →")}
                <p style="margin:0;color:#9ca3af;font-size:12px;">Se não reconheces este email, podes ignorá-lo com segurança.</p>
                """;

            await emailService.SendAsync(new EmailMessage(
                To: normalizedEmail,
                ToName: normalizedEmail,
                Subject: EmailLayout.SubjectSafe($"Convite para a equipa de {branding.SenderName}"),
                HtmlBody: EmailLayout.Render(branding, body, appSettings.ProductName)), cancellationToken);
        }
        catch (Exception ex)
        {
            // Email failure is non-fatal; raw token still returned in response
            logger.LogError(ex, "Failed to send team invite email to {Email}", normalizedEmail);
        }

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
