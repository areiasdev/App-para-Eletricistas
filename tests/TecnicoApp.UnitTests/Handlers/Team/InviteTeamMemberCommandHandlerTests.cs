using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Team.Commands.InviteTeamMember;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Team;

public class InviteTeamMemberCommandHandlerTests
{
    private static (IEmailService email, IAppSettings settings, ILogger<InviteTeamMemberCommandHandler> logger) Deps()
    {
        var email = Substitute.For<IEmailService>();
        var settings = Substitute.For<IAppSettings>();
        settings.BaseUrl.Returns("https://app.example.com");
        var logger = Substitute.For<ILogger<InviteTeamMemberCommandHandler>>();
        return (email, settings, logger);
    }

    [Fact]
    public async Task Handle_self_invite_is_blocked()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        db.Users.Add(owner);
        await db.SaveChangesAsync(CancellationToken.None);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(owner.Id);

        var (email, settings, logger) = Deps();
        var handler = new InviteTeamMemberCommandHandler(db, currentUser, email, settings, logger);

        var result = await handler.Handle(new InviteTeamMemberCommand("owner@x.pt", UserRole.Technician), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        db.TeamMembers.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_inviting_an_existing_account_owner_is_blocked()
    {
        // Circular-ownership guard: someone who already owns their own team can't be
        // folded in as a member of a different team.
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var otherOwner = new User { Email = "otherowner@x.pt", PasswordHash = "h", FullName = "Other Owner" };
        var theirMember = new User
        {
            Email = "theirmember@x.pt", PasswordHash = "h", FullName = "Their Member",
            OwnerId = otherOwner.Id, Role = UserRole.Technician
        };
        db.Users.AddRange(owner, otherOwner, theirMember);
        await db.SaveChangesAsync(CancellationToken.None);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(owner.Id);

        var (email, settings, logger) = Deps();
        var handler = new InviteTeamMemberCommandHandler(db, currentUser, email, settings, logger);

        var result = await handler.Handle(new InviteTeamMemberCommand("otherowner@x.pt", UserRole.Technician), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_technician_cannot_invite_new_members()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var technician = new User
        {
            Email = "tech@x.pt", PasswordHash = "h", FullName = "Tech",
            OwnerId = owner.Id, Role = UserRole.Technician
        };
        db.Users.AddRange(owner, technician);
        await db.SaveChangesAsync(CancellationToken.None);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(technician.Id);

        var (email, settings, logger) = Deps();
        var handler = new InviteTeamMemberCommandHandler(db, currentUser, email, settings, logger);

        var result = await handler.Handle(new InviteTeamMemberCommand("new@x.pt", UserRole.Technician), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
    }

    [Fact]
    public async Task Handle_valid_invite_creates_pending_team_member_with_expiry()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        db.Users.Add(owner);
        await db.SaveChangesAsync(CancellationToken.None);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(owner.Id);

        var (email, settings, logger) = Deps();
        var handler = new InviteTeamMemberCommandHandler(db, currentUser, email, settings, logger);

        var result = await handler.Handle(new InviteTeamMemberCommand("new@x.pt", UserRole.Technician), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var member = db.TeamMembers.Single();
        member.IsAccepted.Should().BeFalse();
        member.InviteTokenExpiresAt.Should().NotBeNull();
        member.InviteTokenExpiresAt!.Value.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
    }
}
