using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Quotes.Commands.CreateQuote;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Quotes;

public class CreateQuoteCommandHandlerTests
{
    private static CreateQuoteCommand ValidFor(Guid clientId) =>
        new(clientId, null, null, null, [new CreateQuoteLineRequest("Serviço", 1, 100m, 23m)]);

    [Fact]
    public async Task Handle_technician_creates_quote_owned_by_team_owner_not_self()
    {
        using var db = TestDb.Create();

        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var technician = new User
        {
            Email = "tech@x.pt", PasswordHash = "h", FullName = "Tech",
            OwnerId = owner.Id, Role = UserRole.Technician
        };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        db.Users.AddRange(owner, technician);
        db.Clients.Add(client);
        await db.SaveChangesAsync(CancellationToken.None);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(technician.Id);

        var logger = Substitute.For<ILogger<CreateQuoteCommandHandler>>();
        var handler = new CreateQuoteCommandHandler(db, currentUser, logger);

        var result = await handler.Handle(ValidFor(client.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var saved = db.Quotes.Single();
        saved.UserId.Should().Be(owner.Id, "quotes are owned by the team, not the individual who created them");
    }

    [Fact]
    public async Task Handle_client_from_a_different_owner_is_forbidden()
    {
        using var db = TestDb.Create();

        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var otherOwner = new User { Email = "other@x.pt", PasswordHash = "h", FullName = "Other" };
        var foreignClient = new Client { Name = "Cliente alheio", UserId = otherOwner.Id };
        db.Users.AddRange(owner, otherOwner);
        db.Clients.Add(foreignClient);
        await db.SaveChangesAsync(CancellationToken.None);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(owner.Id);

        var logger = Substitute.For<ILogger<CreateQuoteCommandHandler>>();
        var handler = new CreateQuoteCommandHandler(db, currentUser, logger);

        var result = await handler.Handle(ValidFor(foreignClient.Id), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
    }

    [Fact]
    public async Task Handle_quote_number_increments_within_owner_and_year()
    {
        using var db = TestDb.Create();

        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        db.Users.Add(owner);
        db.Clients.Add(client);
        await db.SaveChangesAsync(CancellationToken.None);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(owner.Id);

        var logger = Substitute.For<ILogger<CreateQuoteCommandHandler>>();
        var handler = new CreateQuoteCommandHandler(db, currentUser, logger);

        var first = await handler.Handle(ValidFor(client.Id), CancellationToken.None);
        var second = await handler.Handle(ValidFor(client.Id), CancellationToken.None);

        first.Value.Number.Should().EndWith("0001");
        second.Value.Number.Should().EndWith("0002");
    }
}
