using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Team.Commands.UpdateTeamMemberRole;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Team;

public class UpdateTeamMemberRoleCommandHandlerTests
{
    [Fact]
    public async Task Handle_owner_can_change_a_member_role()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner", Role = UserRole.Owner };
        var member = new User { Email = "member@x.pt", PasswordHash = "h", FullName = "Member", OwnerId = owner.Id, Role = UserRole.Technician };
        var teamMember = new TeamMember
        {
            OwnerId = owner.Id, MemberId = member.Id, InviteEmail = member.Email,
            Role = UserRole.Technician, IsAccepted = true,
        };
        db.Users.AddRange(owner, member);
        db.TeamMembers.Add(teamMember);
        await db.SaveChangesAsync(CancellationToken.None);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(owner.Id);
        currentUser.Email.Returns(owner.Email);

        var handler = new UpdateTeamMemberRoleCommandHandler(db, currentUser);

        var result = await handler.Handle(new UpdateTeamMemberRoleCommand(teamMember.Id, UserRole.Admin), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.TeamMembers.Single().Role.Should().Be(UserRole.Admin);
        db.Users.Single(u => u.Id == member.Id).Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public async Task Handle_member_cannot_change_their_own_role()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner", Role = UserRole.Owner };
        var member = new User { Email = "member@x.pt", PasswordHash = "h", FullName = "Member", OwnerId = owner.Id, Role = UserRole.Technician };
        var teamMember = new TeamMember
        {
            OwnerId = owner.Id, MemberId = member.Id, InviteEmail = member.Email,
            Role = UserRole.Technician, IsAccepted = true,
        };
        db.Users.AddRange(owner, member);
        db.TeamMembers.Add(teamMember);
        await db.SaveChangesAsync(CancellationToken.None);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(member.Id);
        currentUser.Email.Returns(member.Email);

        var handler = new UpdateTeamMemberRoleCommandHandler(db, currentUser);

        var result = await handler.Handle(new UpdateTeamMemberRoleCommand(teamMember.Id, UserRole.Admin), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
        db.TeamMembers.Single().Role.Should().Be(UserRole.Technician);
    }

    [Fact]
    public async Task Handle_team_member_from_a_different_owner_is_forbidden()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner", Role = UserRole.Owner };
        var otherOwner = new User { Email = "other@x.pt", PasswordHash = "h", FullName = "Other", Role = UserRole.Owner };
        var foreignMember = new User { Email = "member@x.pt", PasswordHash = "h", FullName = "Member", OwnerId = otherOwner.Id, Role = UserRole.Technician };
        var foreignTeamMember = new TeamMember
        {
            OwnerId = otherOwner.Id, MemberId = foreignMember.Id, InviteEmail = foreignMember.Email,
            Role = UserRole.Technician, IsAccepted = true,
        };
        db.Users.AddRange(owner, otherOwner, foreignMember);
        db.TeamMembers.Add(foreignTeamMember);
        await db.SaveChangesAsync(CancellationToken.None);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(owner.Id);
        currentUser.Email.Returns(owner.Email);

        var handler = new UpdateTeamMemberRoleCommandHandler(db, currentUser);

        var result = await handler.Handle(new UpdateTeamMemberRoleCommand(foreignTeamMember.Id, UserRole.Admin), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
    }
}
