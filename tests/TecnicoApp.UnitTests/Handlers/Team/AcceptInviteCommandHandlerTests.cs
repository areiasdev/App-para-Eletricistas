using FluentAssertions;
using TecnicoApp.Application.Features.Team.Commands.AcceptInvite;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Team;

public class AcceptInviteCommandHandlerTests
{
    private static string HashToken(string token) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));

    [Fact]
    public async Task Handle_expired_invite_is_rejected()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var member = new User { Email = "member@x.pt", PasswordHash = "h", FullName = "member@x.pt", OwnerId = owner.Id };
        var teamMember = new TeamMember
        {
            OwnerId = owner.Id,
            MemberId = member.Id,
            InviteEmail = member.Email,
            InviteTokenHash = HashToken("raw-token"),
            InviteTokenExpiresAt = DateTime.UtcNow.AddDays(-1),
            IsAccepted = false,
        };
        db.Users.AddRange(owner, member);
        db.TeamMembers.Add(teamMember);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new AcceptInviteCommandHandler(db);

        var result = await handler.Handle(
            new AcceptInviteCommand("raw-token", "Novo Nome", "novaPassword123"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        db.TeamMembers.Single().IsAccepted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_valid_invite_activates_the_member()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var member = new User { Email = "member@x.pt", PasswordHash = "h", FullName = "member@x.pt", OwnerId = owner.Id };
        var teamMember = new TeamMember
        {
            OwnerId = owner.Id,
            MemberId = member.Id,
            InviteEmail = member.Email,
            InviteTokenHash = HashToken("raw-token"),
            InviteTokenExpiresAt = DateTime.UtcNow.AddDays(6),
            IsAccepted = false,
        };
        db.Users.AddRange(owner, member);
        db.TeamMembers.Add(teamMember);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new AcceptInviteCommandHandler(db);

        var result = await handler.Handle(
            new AcceptInviteCommand("raw-token", "Novo Nome", "novaPassword123"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var saved = db.TeamMembers.Single();
        saved.IsAccepted.Should().BeTrue();
        saved.InviteTokenHash.Should().BeNull("the token is consumed once used");
        db.Users.Single(u => u.Id == member.Id).FullName.Should().Be("Novo Nome");
    }

    [Fact]
    public async Task Handle_already_accepted_invite_is_rejected()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var member = new User { Email = "member@x.pt", PasswordHash = "h", FullName = "member@x.pt", OwnerId = owner.Id };
        var teamMember = new TeamMember
        {
            OwnerId = owner.Id,
            MemberId = member.Id,
            InviteEmail = member.Email,
            InviteTokenHash = HashToken("raw-token"),
            InviteTokenExpiresAt = DateTime.UtcNow.AddDays(6),
            IsAccepted = true,
        };
        db.Users.AddRange(owner, member);
        db.TeamMembers.Add(teamMember);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new AcceptInviteCommandHandler(db);

        var result = await handler.Handle(
            new AcceptInviteCommand("raw-token", "Novo Nome", "novaPassword123"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
