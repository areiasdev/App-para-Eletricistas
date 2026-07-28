using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.AuditLogs.Queries.GetAuditLogs;
using TecnicoApp.Application.Features.Clients.Commands.DeleteClient;
using TecnicoApp.Application.Features.Equipment.Commands.DeleteEquipment;
using TecnicoApp.Application.Features.Interventions.Commands.DeleteIntervention;
using TecnicoApp.Application.Features.Quotes.Commands.DeleteQuote;
using TecnicoApp.Application.Features.Reports;
using TecnicoApp.Application.Features.Team.Commands.RemoveTeamMember;
using TecnicoApp.Application.Features.Team.Commands.UpdateTeamMemberRole;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Authorization;

// Every destructive or team-management action, plus company-wide financial/audit views,
// must be restricted to Owner/Admin — a Technician or Commercial should be blocked with
// Forbidden even when the target record legitimately belongs to their own team.
public class RoleGateTests
{
    private static (User owner, User technician) SeedTeam(TecnicoApp.Infrastructure.Persistence.AppDbContext db)
    {
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner", Role = UserRole.Owner };
        var technician = new User
        {
            Email = "tech@x.pt", PasswordHash = "h", FullName = "Tech",
            OwnerId = owner.Id, Role = UserRole.Technician
        };
        db.Users.AddRange(owner, technician);
        return (owner, technician);
    }

    private static ICurrentUserService AsUser(User user)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(user.Id);
        currentUser.Email.Returns(user.Email);
        return currentUser;
    }

    [Fact]
    public async Task DeleteClient_technician_is_forbidden()
    {
        using var db = TestDb.Create();
        var (owner, technician) = SeedTeam(db);
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        db.Clients.Add(client);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new DeleteClientCommandHandler(db, AsUser(technician));
        var result = await handler.Handle(new DeleteClientCommand(client.Id), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
        db.Clients.Single().IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteClient_owner_succeeds()
    {
        using var db = TestDb.Create();
        var (owner, _) = SeedTeam(db);
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        db.Clients.Add(client);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new DeleteClientCommandHandler(db, AsUser(owner));
        var result = await handler.Handle(new DeleteClientCommand(client.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.Clients.IgnoreQueryFilters().Single().IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteQuote_technician_is_forbidden()
    {
        using var db = TestDb.Create();
        var (owner, technician) = SeedTeam(db);
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var quote = new Quote { Number = "ORC-2026-0001", ClientId = client.Id, UserId = owner.Id, Status = QuoteStatus.Draft };
        db.Clients.Add(client);
        db.Quotes.Add(quote);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new DeleteQuoteCommandHandler(db, AsUser(technician));
        var result = await handler.Handle(new DeleteQuoteCommand(quote.Id), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
    }

    [Fact]
    public async Task DeleteEquipment_technician_is_forbidden()
    {
        using var db = TestDb.Create();
        var (owner, technician) = SeedTeam(db);
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var equipment = new Equipment { Type = "Caldeira", ClientId = client.Id };
        db.Clients.Add(client);
        db.Equipment.Add(equipment);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new DeleteEquipmentCommandHandler(db, AsUser(technician));
        var result = await handler.Handle(new DeleteEquipmentCommand(equipment.Id), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
    }

    [Fact]
    public async Task DeleteIntervention_technician_is_forbidden()
    {
        using var db = TestDb.Create();
        var (owner, technician) = SeedTeam(db);
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var intervention = new Intervention { Title = "Manutenção", ClientId = client.Id, UserId = owner.Id };
        db.Clients.Add(client);
        db.Interventions.Add(intervention);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new DeleteInterventionCommandHandler(db, AsUser(technician));
        var result = await handler.Handle(new DeleteInterventionCommand(intervention.Id), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
    }

    [Fact]
    public async Task RemoveTeamMember_technician_is_forbidden()
    {
        using var db = TestDb.Create();
        var (owner, technician) = SeedTeam(db);
        var otherMember = new User { Email = "other@x.pt", PasswordHash = "h", FullName = "Other", OwnerId = owner.Id, Role = UserRole.Commercial };
        var teamMember = new TeamMember { OwnerId = owner.Id, MemberId = otherMember.Id, InviteEmail = otherMember.Email, Role = UserRole.Commercial, IsAccepted = true };
        db.Users.Add(otherMember);
        db.TeamMembers.Add(teamMember);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new RemoveTeamMemberCommandHandler(db, AsUser(technician));
        var result = await handler.Handle(new RemoveTeamMemberCommand(teamMember.Id), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
        db.TeamMembers.Single().IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateTeamMemberRole_technician_is_forbidden()
    {
        using var db = TestDb.Create();
        var (owner, technician) = SeedTeam(db);
        var otherMember = new User { Email = "other@x.pt", PasswordHash = "h", FullName = "Other", OwnerId = owner.Id, Role = UserRole.Commercial };
        var teamMember = new TeamMember { OwnerId = owner.Id, MemberId = otherMember.Id, InviteEmail = otherMember.Email, Role = UserRole.Commercial, IsAccepted = true };
        db.Users.Add(otherMember);
        db.TeamMembers.Add(teamMember);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateTeamMemberRoleCommandHandler(db, AsUser(technician));
        var result = await handler.Handle(new UpdateTeamMemberRoleCommand(teamMember.Id, UserRole.Admin), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
        db.TeamMembers.Single().Role.Should().Be(UserRole.Commercial);
    }

    [Fact]
    public async Task UpdateTeamMemberRole_admin_succeeds()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner", Role = UserRole.Owner };
        var admin = new User { Email = "admin@x.pt", PasswordHash = "h", FullName = "Admin", OwnerId = owner.Id, Role = UserRole.Admin };
        var otherMember = new User { Email = "other@x.pt", PasswordHash = "h", FullName = "Other", OwnerId = owner.Id, Role = UserRole.Commercial };
        var teamMember = new TeamMember { OwnerId = owner.Id, MemberId = otherMember.Id, InviteEmail = otherMember.Email, Role = UserRole.Commercial, IsAccepted = true };
        db.Users.AddRange(owner, admin, otherMember);
        db.TeamMembers.Add(teamMember);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateTeamMemberRoleCommandHandler(db, AsUser(admin));
        var result = await handler.Handle(new UpdateTeamMemberRoleCommand(teamMember.Id, UserRole.Technician), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.TeamMembers.Single().Role.Should().Be(UserRole.Technician);
    }

    [Fact]
    public async Task GetProfitabilityReport_technician_is_forbidden()
    {
        using var db = TestDb.Create();
        var (_, technician) = SeedTeam(db);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetProfitabilityReportQueryHandler(db, AsUser(technician));
        var result = await handler.Handle(new GetProfitabilityReportQuery(null, null), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
    }

    [Fact]
    public async Task GetProfitabilityReport_owner_succeeds()
    {
        using var db = TestDb.Create();
        var (owner, _) = SeedTeam(db);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetProfitabilityReportQueryHandler(db, AsUser(owner));
        var result = await handler.Handle(new GetProfitabilityReportQuery(null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetAuditLogs_technician_is_forbidden()
    {
        using var db = TestDb.Create();
        var (_, technician) = SeedTeam(db);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetAuditLogsQueryHandler(db, AsUser(technician));
        var result = await handler.Handle(new GetAuditLogsQuery(), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
    }

    [Fact]
    public async Task GetAuditLogs_admin_succeeds()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner", Role = UserRole.Owner };
        var admin = new User { Email = "admin@x.pt", PasswordHash = "h", FullName = "Admin", OwnerId = owner.Id, Role = UserRole.Admin };
        db.Users.AddRange(owner, admin);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetAuditLogsQueryHandler(db, AsUser(admin));
        var result = await handler.Handle(new GetAuditLogsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
