using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Quotes.Commands.UpdateQuoteStatus;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Quotes;

public class UpdateQuoteStatusCommandHandlerTests
{
    private static ICurrentUserService AsUser(User user)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(user.Id);
        currentUser.Email.Returns(user.Email);
        return currentUser;
    }

    private static (User owner, User technician, Client client, Quote quote) SeedQuote(
        TecnicoApp.Infrastructure.Persistence.AppDbContext db, QuoteStatus status)
    {
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner", Role = UserRole.Owner };
        var technician = new User
        {
            Email = "tech@x.pt", PasswordHash = "h", FullName = "Tech",
            OwnerId = owner.Id, Role = UserRole.Technician
        };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var quote = new Quote { Number = "ORC-2026-0001", ClientId = client.Id, UserId = owner.Id, Status = status };
        db.Users.AddRange(owner, technician);
        db.Clients.Add(client);
        db.Quotes.Add(quote);
        return (owner, technician, client, quote);
    }

    [Theory]
    [InlineData(QuoteStatus.Draft, QuoteStatus.Sent, true)]
    [InlineData(QuoteStatus.Sent, QuoteStatus.Accepted, true)]
    [InlineData(QuoteStatus.Sent, QuoteStatus.Rejected, true)]
    [InlineData(QuoteStatus.Accepted, QuoteStatus.Invoiced, false)] // only via CreateInvoiceFromQuote
    [InlineData(QuoteStatus.Sent, QuoteStatus.Draft, true)]        // allowed recall
    [InlineData(QuoteStatus.Draft, QuoteStatus.Accepted, false)]   // can't skip Sent
    [InlineData(QuoteStatus.Draft, QuoteStatus.Rejected, false)]
    [InlineData(QuoteStatus.Accepted, QuoteStatus.Sent, false)]    // no going back once accepted
    [InlineData(QuoteStatus.Rejected, QuoteStatus.Sent, false)]    // rejected is terminal
    [InlineData(QuoteStatus.Invoiced, QuoteStatus.Draft, false)]   // invoiced is terminal
    [InlineData(QuoteStatus.Draft, QuoteStatus.Draft, false)]      // no-op transition not allowed
    public async Task Handle_transitions_are_validated(QuoteStatus from, QuoteStatus to, bool expectedValid)
    {
        using var db = TestDb.Create();
        var (owner, _, _, quote) = SeedQuote(db, from);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateQuoteStatusCommandHandler(db, AsUser(owner));
        var result = await handler.Handle(new UpdateQuoteStatusCommand(quote.Id, to), CancellationToken.None);

        result.IsSuccess.Should().Be(expectedValid);
        var saved = db.Quotes.Single();
        saved.Status.Should().Be(expectedValid ? to : from);
    }

    [Fact]
    public async Task Handle_records_who_made_the_change()
    {
        using var db = TestDb.Create();
        var (owner, _, _, quote) = SeedQuote(db, QuoteStatus.Draft);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateQuoteStatusCommandHandler(db, AsUser(owner));
        var result = await handler.Handle(new UpdateQuoteStatusCommand(quote.Id, QuoteStatus.Sent), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.Quotes.Single().ModifiedBy.Should().Be(owner.Email);
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

        var handler = new UpdateQuoteStatusCommandHandler(db, AsUser(owner));
        var result = await handler.Handle(new UpdateQuoteStatusCommand(foreignQuote.Id, QuoteStatus.Sent), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
        db.Quotes.Single().Status.Should().Be(QuoteStatus.Draft);
    }

    [Fact]
    public async Task Handle_technician_can_update_owners_quote_status()
    {
        using var db = TestDb.Create();
        var (_, technician, _, quote) = SeedQuote(db, QuoteStatus.Draft);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateQuoteStatusCommandHandler(db, AsUser(technician));
        var result = await handler.Handle(new UpdateQuoteStatusCommand(quote.Id, QuoteStatus.Sent), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.Quotes.Single().Status.Should().Be(QuoteStatus.Sent);
    }
}
