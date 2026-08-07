using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Quotes.Commands.SignQuote;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Quotes;

public class SignQuoteCommandHandlerTests
{
    private const string ValidPngDataUrl = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUg==";

    private static ICurrentUserService AsUser(User user)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(user.Id);
        currentUser.Email.Returns(user.Email);
        return currentUser;
    }

    private static (User owner, Client client, Quote quote) SeedQuote(
        TecnicoApp.Infrastructure.Persistence.AppDbContext db, QuoteStatus status)
    {
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner", Role = UserRole.Owner };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var quote = new Quote { Number = "ORC-2026-0001", ClientId = client.Id, UserId = owner.Id, Status = status };
        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Quotes.Add(quote);
        return (owner, client, quote);
    }

    [Fact]
    public async Task Handle_valid_signature_on_sent_quote_signs_and_auto_accepts()
    {
        using var db = TestDb.Create();
        var (owner, _, quote) = SeedQuote(db, QuoteStatus.Sent);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new SignQuoteCommandHandler(db, AsUser(owner));
        var result = await handler.Handle(new SignQuoteCommand(quote.Id, ValidPngDataUrl), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var saved = db.Quotes.Single();
        saved.Status.Should().Be(QuoteStatus.Accepted, "signing a Sent quote should automatically accept it");
        saved.SignatureUrl.Should().Be(ValidPngDataUrl);
        saved.SignedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_signing_an_already_accepted_quote_keeps_it_accepted()
    {
        using var db = TestDb.Create();
        var (owner, _, quote) = SeedQuote(db, QuoteStatus.Accepted);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new SignQuoteCommandHandler(db, AsUser(owner));
        var result = await handler.Handle(new SignQuoteCommand(quote.Id, ValidPngDataUrl), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.Quotes.Single().Status.Should().Be(QuoteStatus.Accepted);
    }

    [Fact]
    public async Task Handle_draft_quote_cannot_be_signed()
    {
        using var db = TestDb.Create();
        var (owner, _, quote) = SeedQuote(db, QuoteStatus.Draft);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new SignQuoteCommandHandler(db, AsUser(owner));
        var result = await handler.Handle(new SignQuoteCommand(quote.Id, ValidPngDataUrl), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Invalid);
        var saved = db.Quotes.Single();
        saved.SignatureUrl.Should().BeNull();
        saved.SignedAt.Should().BeNull();
    }

    [Theory]
    [InlineData("not-a-data-url")]
    [InlineData("data:image/svg+xml;base64,PHN2ZyB4bWxucz0=")] // SVG explicitly disallowed — can embed script
    [InlineData("data:image/png;base64,not valid base64!!")]   // invalid base64 charset (space, !)
    [InlineData("data:text/plain;base64,aGVsbG8=")]            // not an image mime type at all
    [InlineData("")]
    public async Task Handle_rejects_malformed_or_disallowed_signature_data_urls(string badDataUrl)
    {
        using var db = TestDb.Create();
        var (owner, _, quote) = SeedQuote(db, QuoteStatus.Sent);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new SignQuoteCommandHandler(db, AsUser(owner));
        var result = await handler.Handle(new SignQuoteCommand(quote.Id, badDataUrl), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Invalid);
        var saved = db.Quotes.Single();
        saved.Status.Should().Be(QuoteStatus.Sent, "an invalid signature must not mutate the quote's status");
        saved.SignatureUrl.Should().BeNull();
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
            Number = "ORC-2026-0001", ClientId = foreignClient.Id, UserId = otherOwner.Id, Status = QuoteStatus.Sent
        };
        db.Users.AddRange(owner, otherOwner);
        db.Clients.Add(foreignClient);
        db.Quotes.Add(foreignQuote);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new SignQuoteCommandHandler(db, AsUser(owner));
        var result = await handler.Handle(new SignQuoteCommand(foreignQuote.Id, ValidPngDataUrl), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
    }

    [Fact]
    public async Task Handle_nonexistent_quote_returns_not_found()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        db.Users.Add(owner);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new SignQuoteCommandHandler(db, AsUser(owner));
        var result = await handler.Handle(new SignQuoteCommand(Guid.NewGuid(), ValidPngDataUrl), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
    }
}
