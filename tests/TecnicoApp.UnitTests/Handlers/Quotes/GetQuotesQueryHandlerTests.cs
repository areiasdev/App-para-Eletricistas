using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Quotes.Queries.GetQuotes;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Quotes;

public class GetQuotesQueryHandlerTests
{
    private static ICurrentUserService AsUser(User user)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(user.Id);
        return currentUser;
    }

    [Fact]
    public async Task Handle_only_returns_quotes_belonging_to_callers_tenant()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var otherOwner = new User { Email = "other@x.pt", PasswordHash = "h", FullName = "Other" };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var otherClient = new Client { Name = "Cliente B", UserId = otherOwner.Id };
        var mine = new Quote { Number = "ORC-2026-0001", ClientId = client.Id, UserId = owner.Id, Status = QuoteStatus.Draft };
        var foreign = new Quote { Number = "ORC-2026-0002", ClientId = otherClient.Id, UserId = otherOwner.Id, Status = QuoteStatus.Draft };
        db.Users.AddRange(owner, otherOwner);
        db.Clients.AddRange(client, otherClient);
        db.Quotes.AddRange(mine, foreign);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetQuotesQueryHandler(db, AsUser(owner));
        var result = await handler.Handle(new GetQuotesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(q => q.Id == mine.Id);
        result.Value.Items.Should().NotContain(q => q.Id == foreign.Id);
    }

    [Fact]
    public async Task Handle_team_member_sees_owners_quotes()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var technician = new User
        {
            Email = "tech@x.pt", PasswordHash = "h", FullName = "Tech",
            OwnerId = owner.Id, Role = UserRole.Technician
        };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var quote = new Quote { Number = "ORC-2026-0001", ClientId = client.Id, UserId = owner.Id, Status = QuoteStatus.Draft };
        db.Users.AddRange(owner, technician);
        db.Clients.Add(client);
        db.Quotes.Add(quote);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetQuotesQueryHandler(db, AsUser(technician));
        var result = await handler.Handle(new GetQuotesQuery(), CancellationToken.None);

        result.Value.Items.Should().ContainSingle(q => q.Id == quote.Id);
    }

    [Fact]
    public async Task Handle_filters_by_status()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var draft = new Quote { Number = "ORC-2026-0001", ClientId = client.Id, UserId = owner.Id, Status = QuoteStatus.Draft };
        var sent = new Quote { Number = "ORC-2026-0002", ClientId = client.Id, UserId = owner.Id, Status = QuoteStatus.Sent };
        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Quotes.AddRange(draft, sent);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetQuotesQueryHandler(db, AsUser(owner));
        var result = await handler.Handle(new GetQuotesQuery(Status: QuoteStatus.Sent), CancellationToken.None);

        result.Value.Items.Should().ContainSingle(q => q.Id == sent.Id);
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_filters_by_client_id()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var clientA = new Client { Name = "Cliente A", UserId = owner.Id };
        var clientB = new Client { Name = "Cliente B", UserId = owner.Id };
        var quoteA = new Quote { Number = "ORC-2026-0001", ClientId = clientA.Id, UserId = owner.Id, Status = QuoteStatus.Draft };
        var quoteB = new Quote { Number = "ORC-2026-0002", ClientId = clientB.Id, UserId = owner.Id, Status = QuoteStatus.Draft };
        db.Users.Add(owner);
        db.Clients.AddRange(clientA, clientB);
        db.Quotes.AddRange(quoteA, quoteB);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetQuotesQueryHandler(db, AsUser(owner));
        var result = await handler.Handle(new GetQuotesQuery(ClientId: clientB.Id), CancellationToken.None);

        result.Value.Items.Should().ContainSingle(q => q.Id == quoteB.Id);
    }

    [Fact]
    public async Task Handle_search_matches_quote_number_or_client_name_case_insensitively()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var client = new Client { Name = "Eletricidade Rápida", UserId = owner.Id };
        var otherClient = new Client { Name = "Outra Empresa", UserId = owner.Id };
        var matchByClient = new Quote { Number = "ORC-2026-0001", ClientId = client.Id, UserId = owner.Id, Status = QuoteStatus.Draft };
        var noMatch = new Quote { Number = "ORC-2026-0002", ClientId = otherClient.Id, UserId = owner.Id, Status = QuoteStatus.Draft };
        db.Users.Add(owner);
        db.Clients.AddRange(client, otherClient);
        db.Quotes.AddRange(matchByClient, noMatch);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetQuotesQueryHandler(db, AsUser(owner));
        var result = await handler.Handle(new GetQuotesQuery(Search: "rápida"), CancellationToken.None);

        result.Value.Items.Should().ContainSingle(q => q.Id == matchByClient.Id);
    }

    [Fact]
    public async Task Handle_paginates_results()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        db.Users.Add(owner);
        db.Clients.Add(client);
        for (var i = 1; i <= 5; i++)
            db.Quotes.Add(new Quote { Number = $"ORC-2026-{i:0000}", ClientId = client.Id, UserId = owner.Id, Status = QuoteStatus.Draft });
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetQuotesQueryHandler(db, AsUser(owner));
        var result = await handler.Handle(new GetQuotesQuery(Page: 2, PageSize: 2), CancellationToken.None);

        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(5);
        result.Value.Page.Should().Be(2);
    }
}
