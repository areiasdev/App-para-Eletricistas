using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.AuditLogs.Queries.GetAuditLogs;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.AuditLogs;

public class GetAuditLogsQueryHandlerTests
{
    private static ICurrentUserService AsUser(User user)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(user.Id);
        currentUser.Email.Returns(user.Email);
        return currentUser;
    }

    [Fact]
    public async Task Handle_technician_is_forbidden()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var technician = new User
        {
            Email = "tech@x.pt", PasswordHash = "h", FullName = "Tech",
            OwnerId = owner.Id, Role = UserRole.Technician,
        };
        db.Users.AddRange(owner, technician);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetAuditLogsQueryHandler(db, AsUser(technician));
        var result = await handler.Handle(new GetAuditLogsQuery(), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
    }

    [Fact]
    public async Task Handle_never_returns_another_tenants_rows()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var otherOwner = new User { Email = "other@x.pt", PasswordHash = "h", FullName = "Other" };
        db.Users.AddRange(owner, otherOwner);
        db.AuditLogs.AddRange(
            new AuditLog { EntityType = "Client", EntityId = "1", Action = "Created", UserId = owner.Id },
            new AuditLog { EntityType = "Client", EntityId = "2", Action = "Created", UserId = otherOwner.Id });
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetAuditLogsQueryHandler(db, AsUser(owner));
        var result = await handler.Handle(new GetAuditLogsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();
        result.Value.Items.Single().UserId.Should().Be(owner.Id);
    }

    [Fact]
    public async Task Handle_rows_with_no_UserId_are_never_shown_to_anyone()
    {
        // A row with a null UserId isn't attributable to any tenant. Treating "no owner"
        // as "visible to everyone" would leak it into every team's audit log.
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        db.Users.Add(owner);
        db.AuditLogs.Add(new AuditLog { EntityType = "Client", EntityId = "1", Action = "Created", UserId = null });
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetAuditLogsQueryHandler(db, AsUser(owner));
        var result = await handler.Handle(new GetAuditLogsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_team_member_actions_are_visible_to_the_owner()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var technician = new User
        {
            Email = "tech@x.pt", PasswordHash = "h", FullName = "Tech",
            OwnerId = owner.Id, Role = UserRole.Technician,
        };
        db.Users.AddRange(owner, technician);
        db.TeamMembers.Add(new TeamMember
        {
            OwnerId = owner.Id, MemberId = technician.Id, Role = UserRole.Technician,
            InviteEmail = technician.Email, InviteTokenHash = "h", IsAccepted = true,
        });
        db.AuditLogs.Add(new AuditLog { EntityType = "Client", EntityId = "1", Action = "Created", UserId = technician.Id });
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetAuditLogsQueryHandler(db, AsUser(owner));
        var result = await handler.Handle(new GetAuditLogsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(l => l.UserId == technician.Id);
    }

    [Fact]
    public async Task Handle_filters_by_entity_type()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        db.Users.Add(owner);
        db.AuditLogs.AddRange(
            new AuditLog { EntityType = "Client", EntityId = "1", Action = "Created", UserId = owner.Id },
            new AuditLog { EntityType = "Quote", EntityId = "2", Action = "Created", UserId = owner.Id });
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetAuditLogsQueryHandler(db, AsUser(owner));
        var result = await handler.Handle(new GetAuditLogsQuery(EntityType: "Quote"), CancellationToken.None);

        result.Value.Items.Should().ContainSingle(l => l.EntityType == "Quote");
    }
}
