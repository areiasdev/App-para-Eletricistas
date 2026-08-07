using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Team.Queries.GetTeam;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Team;

public class GetTeamQueryHandlerTests
{
    [Fact]
    public async Task Handle_owner_sees_their_team_members()
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

        var handler = new GetTeamQueryHandler(db, currentUser);

        var result = await handler.Handle(new GetTeamQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        var dto = result.Value.Single();
        dto.Id.Should().Be(teamMember.Id);
        dto.MemberId.Should().Be(member.Id);
        dto.FullName.Should().Be("Member");
        dto.Email.Should().Be(member.Email);
        dto.IsAccepted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_team_member_sees_the_owners_team_not_just_themselves()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner", Role = UserRole.Owner };
        var memberA = new User { Email = "a@x.pt", PasswordHash = "h", FullName = "Member A", OwnerId = owner.Id, Role = UserRole.Technician };
        var memberB = new User { Email = "b@x.pt", PasswordHash = "h", FullName = "Member B", OwnerId = owner.Id, Role = UserRole.Technician };
        var teamMemberA = new TeamMember { OwnerId = owner.Id, MemberId = memberA.Id, InviteEmail = memberA.Email, Role = UserRole.Technician, IsAccepted = true };
        var teamMemberB = new TeamMember { OwnerId = owner.Id, MemberId = memberB.Id, InviteEmail = memberB.Email, Role = UserRole.Technician, IsAccepted = true };
        db.Users.AddRange(owner, memberA, memberB);
        db.TeamMembers.AddRange(teamMemberA, teamMemberB);
        await db.SaveChangesAsync(CancellationToken.None);

        // memberA queries as a logged-in team member, not the owner.
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(memberA.Id);

        var handler = new GetTeamQueryHandler(db, currentUser);

        var result = await handler.Handle(new GetTeamQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Select(m => m.MemberId).Should().BeEquivalentTo(new[] { memberA.Id, memberB.Id });
    }

    [Fact]
    public async Task Handle_excludes_soft_deleted_members_and_other_owners_teams()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner", Role = UserRole.Owner };
        var deletedMember = new User { Email = "gone@x.pt", PasswordHash = "h", FullName = "Gone", OwnerId = owner.Id, Role = UserRole.Technician, IsDeleted = true };
        var teamMemberDeleted = new TeamMember { OwnerId = owner.Id, MemberId = deletedMember.Id, InviteEmail = deletedMember.Email, Role = UserRole.Technician, IsAccepted = true };

        var otherOwner = new User { Email = "other@x.pt", PasswordHash = "h", FullName = "Other", Role = UserRole.Owner };
        var foreignMember = new User { Email = "foreign@x.pt", PasswordHash = "h", FullName = "Foreign", OwnerId = otherOwner.Id, Role = UserRole.Technician };
        var foreignTeamMember = new TeamMember { OwnerId = otherOwner.Id, MemberId = foreignMember.Id, InviteEmail = foreignMember.Email, Role = UserRole.Technician, IsAccepted = true };

        db.Users.AddRange(owner, deletedMember, otherOwner, foreignMember);
        db.TeamMembers.AddRange(teamMemberDeleted, foreignTeamMember);
        await db.SaveChangesAsync(CancellationToken.None);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(owner.Id);

        var handler = new GetTeamQueryHandler(db, currentUser);

        var result = await handler.Handle(new GetTeamQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
