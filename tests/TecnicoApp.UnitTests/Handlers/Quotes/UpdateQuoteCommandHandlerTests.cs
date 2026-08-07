using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Quotes.Commands.CreateQuote;
using TecnicoApp.Application.Features.Quotes.Commands.UpdateQuote;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Quotes;

public class UpdateQuoteCommandHandlerTests
{
    private static ICurrentUserService AsUser(User user)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(user.Id);
        currentUser.Email.Returns(user.Email);
        return currentUser;
    }

    private static (User owner, Client client, Quote quote) SeedDraftQuote(
        TecnicoApp.Infrastructure.Persistence.AppDbContext db)
    {
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner", Role = UserRole.Owner };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var quote = new Quote { Number = "ORC-2026-0001", ClientId = client.Id, UserId = owner.Id, Status = QuoteStatus.Draft };
        quote.Lines.Add(new QuoteLine { Description = "Linha original", Quantity = 1, UnitPrice = 50m, VatRate = 23m, QuoteId = quote.Id });
        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Quotes.Add(quote);
        return (owner, client, quote);
    }

    [Fact]
    public async Task Handle_draft_quote_replaces_lines_and_recalculates_totals()
    {
        // Two contexts over the same underlying store, not one shared context: replacing an
        // already-persisted collection (quote.Lines.Clear() + re-add) needs the handler to load
        // the original line fresh rather than reuse an instance already tracked by the seeding
        // context, matching how production works (each request gets its own scoped DbContext).
        var dbName = Guid.NewGuid().ToString();
        using var seedDb = TestDb.Create(dbName);
        var (owner, client, quote) = SeedDraftQuote(seedDb);
        await seedDb.SaveChangesAsync(CancellationToken.None);

        using var db = TestDb.Create(dbName);

        var command = new UpdateQuoteCommand(
            quote.Id, client.Id, Discount: 10m, Notes: "Notas novas", ValidUntil: null,
            Lines: [new CreateQuoteLineRequest("Serviço novo", 2, 100m, 23m)]);

        var handler = new UpdateQuoteCommandHandler(db, AsUser(owner));
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Lines.Should().ContainSingle(l => l.Description == "Serviço novo");
        // SubTotal = 2 * 100 = 200; VAT = 200 * 0.23 = 46; Total = 200 + 46 - 10 discount = 236
        result.Value.SubTotal.Should().Be(200m);
        result.Value.VatTotal.Should().Be(46m);
        result.Value.Total.Should().Be(236m);
    }

    [Fact]
    public async Task Handle_non_draft_quote_cannot_be_edited()
    {
        using var db = TestDb.Create();
        var (owner, client, quote) = SeedDraftQuote(db);
        quote.Status = QuoteStatus.Sent;
        await db.SaveChangesAsync(CancellationToken.None);

        var command = new UpdateQuoteCommand(
            quote.Id, client.Id, Discount: null, Notes: null, ValidUntil: null,
            Lines: [new CreateQuoteLineRequest("Nova linha", 1, 10m, 23m)]);

        var handler = new UpdateQuoteCommandHandler(db, AsUser(owner));
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.Error);
    }

    [Fact]
    public async Task Handle_quote_from_a_different_owner_is_forbidden()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var otherOwner = new User { Email = "other@x.pt", PasswordHash = "h", FullName = "Other" };
        var foreignClient = new Client { Name = "Cliente alheio", UserId = otherOwner.Id };
        var foreignQuote = new Quote
        {
            Number = "ORC-2026-0001", ClientId = foreignClient.Id, UserId = otherOwner.Id, Status = QuoteStatus.Draft
        };
        db.Users.AddRange(owner, otherOwner);
        db.Clients.Add(foreignClient);
        db.Quotes.Add(foreignQuote);
        await db.SaveChangesAsync(CancellationToken.None);

        var command = new UpdateQuoteCommand(
            foreignQuote.Id, foreignClient.Id, Discount: null, Notes: null, ValidUntil: null,
            Lines: [new CreateQuoteLineRequest("X", 1, 10m, 23m)]);

        var handler = new UpdateQuoteCommandHandler(db, AsUser(owner));
        var result = await handler.Handle(command, CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
    }

    [Fact]
    public async Task Handle_reassigning_to_a_client_from_a_different_owner_is_forbidden()
    {
        using var db = TestDb.Create();
        var (owner, _, quote) = SeedDraftQuote(db);
        var otherOwner = new User { Email = "other@x.pt", PasswordHash = "h", FullName = "Other" };
        var foreignClient = new Client { Name = "Cliente alheio", UserId = otherOwner.Id };
        db.Users.Add(otherOwner);
        db.Clients.Add(foreignClient);
        await db.SaveChangesAsync(CancellationToken.None);

        var command = new UpdateQuoteCommand(
            quote.Id, foreignClient.Id, Discount: null, Notes: null, ValidUntil: null,
            Lines: [new CreateQuoteLineRequest("X", 1, 10m, 23m)]);

        var handler = new UpdateQuoteCommandHandler(db, AsUser(owner));
        var result = await handler.Handle(command, CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
        db.Quotes.Single().ClientId.Should().NotBe(foreignClient.Id, "the client swap must not be persisted when forbidden");
    }
}
