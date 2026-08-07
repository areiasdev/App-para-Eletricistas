using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Interventions.Commands.UpdateInterventionStatus;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Interventions;

public class UpdateInterventionStatusCommandHandlerTests
{
    private static ICurrentUserService AsUser(User user)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(user.Id);
        currentUser.Email.Returns(user.Email);
        return currentUser;
    }

    private static async Task<(TecnicoApp.Infrastructure.Persistence.AppDbContext db, User owner, Client client, Intervention iv)> Seed(
        InterventionStatus initialStatus)
    {
        var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var iv = new Intervention { Title = "T", ClientId = client.Id, UserId = owner.Id, Status = initialStatus };
        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Interventions.Add(iv);
        await db.SaveChangesAsync(CancellationToken.None);
        return (db, owner, client, iv);
    }

    [Theory]
    [InlineData(InterventionStatus.Scheduled, InterventionStatus.InProgress, true)]
    [InlineData(InterventionStatus.InProgress, InterventionStatus.Completed, true)]
    [InlineData(InterventionStatus.InProgress, InterventionStatus.Scheduled, true)]
    [InlineData(InterventionStatus.Scheduled, InterventionStatus.Completed, false)]
    [InlineData(InterventionStatus.Completed, InterventionStatus.InProgress, false)]
    [InlineData(InterventionStatus.Completed, InterventionStatus.Scheduled, false)]
    public async Task Handle_only_the_defined_transitions_are_allowed(
        InterventionStatus from, InterventionStatus to, bool shouldSucceed)
    {
        var (db, owner, _, iv) = await Seed(from);
        using var _db = db;

        var handler = new UpdateInterventionStatusCommandHandler(db, AsUser(owner));
        var result = await handler.Handle(new UpdateInterventionStatusCommand(iv.Id, to), CancellationToken.None);

        result.IsSuccess.Should().Be(shouldSucceed);
        db.Interventions.Single().Status.Should().Be(shouldSucceed ? to : from);
    }

    [Fact]
    public async Task Handle_completing_sets_CompletedAt_and_Completed_is_terminal()
    {
        var (db, owner, _, iv) = await Seed(InterventionStatus.InProgress);
        using var _db = db;

        var handler = new UpdateInterventionStatusCommandHandler(db, AsUser(owner));
        await handler.Handle(new UpdateInterventionStatusCommand(iv.Id, InterventionStatus.Completed), CancellationToken.None);
        db.Interventions.Single().CompletedAt.Should().NotBeNull();

        var rollbackAttempt = await handler.Handle(new UpdateInterventionStatusCommand(iv.Id, InterventionStatus.Scheduled), CancellationToken.None);
        rollbackAttempt.IsSuccess.Should().BeFalse("Completed is a terminal state — there is no valid transition out of it");
        db.Interventions.Single().Status.Should().Be(InterventionStatus.Completed);
    }

    [Fact]
    public async Task Handle_cross_tenant_access_is_forbidden()
    {
        var (db, _, _, iv) = await Seed(InterventionStatus.Scheduled);
        using var _db = db;
        var stranger = new User { Email = "stranger@x.pt", PasswordHash = "h", FullName = "Stranger" };
        db.Users.Add(stranger);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateInterventionStatusCommandHandler(db, AsUser(stranger));
        var result = await handler.Handle(new UpdateInterventionStatusCommand(iv.Id, InterventionStatus.InProgress), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
        db.Interventions.Single().Status.Should().Be(InterventionStatus.Scheduled);
    }

    [Fact]
    public async Task Handle_unknown_intervention_returns_not_found()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        db.Users.Add(owner);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateInterventionStatusCommandHandler(db, AsUser(owner));
        var result = await handler.Handle(new UpdateInterventionStatusCommand(Guid.NewGuid(), InterventionStatus.InProgress), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
    }
}
