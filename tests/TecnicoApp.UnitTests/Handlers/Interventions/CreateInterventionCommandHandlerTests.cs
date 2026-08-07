using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Interventions.Commands.CreateIntervention;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Interventions;

public class CreateInterventionCommandHandlerTests
{
    private static ICurrentUserService AsUser(User user)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(user.Id);
        currentUser.Email.Returns(user.Email);
        return currentUser;
    }

    private static CreateInterventionCommand Valid(Guid clientId, Guid? quoteId = null, Guid? assignedTo = null) =>
        new("Revisão", null, clientId, null, quoteId, [], null, null, assignedTo);

    [Fact]
    public async Task Handle_client_from_another_owner_is_forbidden()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var otherOwner = new User { Email = "other@x.pt", PasswordHash = "h", FullName = "Other" };
        var foreignClient = new Client { Name = "Cliente Alheio", UserId = otherOwner.Id };
        db.Users.AddRange(owner, otherOwner);
        db.Clients.Add(foreignClient);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateInterventionCommandHandler(db, AsUser(owner));
        var result = await handler.Handle(Valid(foreignClient.Id), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
        db.Interventions.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_quote_from_a_different_client_is_rejected()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var otherClient = new Client { Name = "Cliente B", UserId = owner.Id };
        var quoteForOtherClient = new Quote { Number = "ORC-1", UserId = owner.Id, ClientId = otherClient.Id };
        db.Users.Add(owner);
        db.Clients.AddRange(client, otherClient);
        db.Quotes.Add(quoteForOtherClient);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateInterventionCommandHandler(db, AsUser(owner));
        var result = await handler.Handle(Valid(client.Id, quoteForOtherClient.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("não pertence ao cliente"));
        db.Interventions.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_valid_quote_for_the_same_client_is_linked_so_revenue_can_be_tracked()
    {
        // Regression test: the frontend used to have no UI to ever set this field, so
        // revenue reports (GetProfitabilityReportQuery) always showed 0 for everyone.
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var quote = new Quote { Number = "ORC-1", UserId = owner.Id, ClientId = client.Id };
        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Quotes.Add(quote);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateInterventionCommandHandler(db, AsUser(owner));
        var result = await handler.Handle(Valid(client.Id, quote.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.Interventions.Single().QuoteId.Should().Be(quote.Id);
    }

    [Fact]
    public async Task Handle_technician_creating_own_intervention_is_auto_assigned_to_self()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var technician = new User
        {
            Email = "tech@x.pt", PasswordHash = "h", FullName = "Tech",
            OwnerId = owner.Id, Role = UserRole.Technician,
        };
        var otherTech = new User
        {
            Email = "tech2@x.pt", PasswordHash = "h", FullName = "Tech2",
            OwnerId = owner.Id, Role = UserRole.Technician,
        };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        db.Users.AddRange(owner, technician, otherTech);
        db.Clients.Add(client);
        db.TeamMembers.Add(new TeamMember
        {
            OwnerId = owner.Id, MemberId = otherTech.Id, Role = UserRole.Technician,
            InviteEmail = otherTech.Email, InviteTokenHash = "h", IsAccepted = true,
        });
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateInterventionCommandHandler(db, AsUser(technician));
        // Attempts to assign to someone else — should be silently overridden to self.
        var result = await handler.Handle(Valid(client.Id, assignedTo: otherTech.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.Interventions.Single().AssignedToUserId.Should().Be(technician.Id);
    }

    [Fact]
    public async Task Handle_assigning_to_a_non_team_member_fails()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var stranger = new User { Email = "stranger@x.pt", PasswordHash = "h", FullName = "Stranger" };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        db.Users.AddRange(owner, stranger);
        db.Clients.Add(client);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateInterventionCommandHandler(db, AsUser(owner));
        var result = await handler.Handle(Valid(client.Id, assignedTo: stranger.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        db.Interventions.Should().BeEmpty();
    }
}
