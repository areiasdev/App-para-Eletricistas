using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Quotes.Public;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.Infrastructure.Persistence;
using TecnicoApp.Infrastructure.Services;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Quotes;

public class PublicQuoteHandlersTests
{
    private const string Signature = "data:image/png;base64,iVBORw0KGgo=";

    private static (Quote quote, string token) Seed(AppDbContext db, QuoteStatus status = QuoteStatus.Sent, DateTime? validUntil = null)
    {
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner", CompanyName = "Eletro Lda" };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var quote = new Quote
        {
            Number = "ORC-2026-0001", ClientId = client.Id, UserId = owner.Id, Status = status,
            ValidUntil = validUntil,
            Lines = [new QuoteLine { Description = "Quadro", Quantity = 1, UnitPrice = 100 }],
        };
        var token = new QuoteApprovalLinkService(FakeConfig.Create()).GetOrCreateToken(quote);
        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Quotes.Add(quote);
        db.SaveChanges();
        return (quote, token);
    }

    private static AcceptQuoteOnlineCommandHandler AcceptHandler(AppDbContext db, IEmailService? email = null) =>
        new(db, email ?? Substitute.For<IEmailService>(), Substitute.For<IAppSettings>(),
            Substitute.For<ILogger<AcceptQuoteOnlineCommandHandler>>());

    [Fact]
    public async Task Accept_marks_quote_accepted_with_signature_and_notifies_team()
    {
        using var db = TestDb.Create();
        var (quote, token) = Seed(db);
        var email = Substitute.For<IEmailService>();

        var result = await AcceptHandler(db, email).Handle(
            new AcceptQuoteOnlineCommand(token, "Maria Cliente", Signature), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var saved = db.Quotes.Single();
        saved.Status.Should().Be(QuoteStatus.Accepted);
        saved.AcceptedByName.Should().Be("Maria Cliente");
        saved.SignatureUrl.Should().Be(Signature);
        saved.ClientDecisionAt.Should().NotBeNull();
        await email.Received(1).SendAsync(Arg.Is<EmailMessage>(m => m.To == "owner@x.pt"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Accept_after_validity_date_is_refused()
    {
        using var db = TestDb.Create();
        var (_, token) = Seed(db, validUntil: DateTime.UtcNow.AddDays(1));
        db.Quotes.Single().ValidUntil = DateTime.UtcNow.AddDays(-2);
        db.SaveChanges();

        var result = await AcceptHandler(db).Handle(new AcceptQuoteOnlineCommand(token, "Maria", Signature), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        db.Quotes.Single().Status.Should().Be(QuoteStatus.Sent);
    }

    [Fact]
    public async Task Accept_requires_a_raster_signature()
    {
        using var db = TestDb.Create();
        var (_, token) = Seed(db);

        var result = await AcceptHandler(db).Handle(
            new AcceptQuoteOnlineCommand(token, "Maria", "data:image/svg+xml;base64,PHN2Zz4="), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Invalid);
    }

    [Theory]
    [InlineData(QuoteStatus.Draft)]
    [InlineData(QuoteStatus.Accepted)]
    [InlineData(QuoteStatus.Invoiced)]
    public async Task Accept_only_works_while_quote_awaits_an_answer(QuoteStatus status)
    {
        using var db = TestDb.Create();
        var (_, token) = Seed(db, status);

        var result = await AcceptHandler(db).Handle(new AcceptQuoteOnlineCommand(token, "Maria", Signature), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        db.Quotes.Single().Status.Should().Be(status);
    }

    [Fact]
    public async Task Unknown_token_is_not_found()
    {
        using var db = TestDb.Create();
        Seed(db);

        var result = await new GetPublicQuoteQueryHandler(db).Handle(new GetPublicQuoteQuery("nope"), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
    }

    [Fact]
    public async Task Get_returns_company_and_lines_for_the_client()
    {
        using var db = TestDb.Create();
        var (_, token) = Seed(db);

        var result = await new GetPublicQuoteQueryHandler(db).Handle(new GetPublicQuoteQuery(token), CancellationToken.None);

        result.Value.IssuerName.Should().Be("Eletro Lda");
        result.Value.Lines.Should().ContainSingle();
        result.Value.Total.Should().Be(123m);
    }

    [Fact]
    public async Task Reject_records_reason()
    {
        using var db = TestDb.Create();
        var (_, token) = Seed(db);
        var handler = new RejectQuoteOnlineCommandHandler(db, Substitute.For<IEmailService>(), Substitute.For<IAppSettings>(),
            Substitute.For<ILogger<RejectQuoteOnlineCommandHandler>>());

        var result = await handler.Handle(new RejectQuoteOnlineCommand(token, "Muito caro"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var saved = db.Quotes.Single();
        saved.Status.Should().Be(QuoteStatus.Rejected);
        saved.RejectionReason.Should().Be("Muito caro");
    }

    [Fact]
    public void Approval_token_is_stable_across_resends()
    {
        var service = new QuoteApprovalLinkService(FakeConfig.Create());
        var quote = new Quote { Number = "ORC-1" };

        var first = service.GetOrCreateToken(quote);
        var second = service.GetOrCreateToken(quote);

        second.Should().Be(first, "a link already emailed to the client must keep working");
    }

    [Fact]
    public void Approval_token_survives_a_database_round_trip_of_its_expiry()
    {
        // Postgres stores microseconds; a sub-microsecond expiry would re-derive a different token
        // after reload and break the link already emailed.
        var service = new QuoteApprovalLinkService(FakeConfig.Create());
        var quote = new Quote { Number = "ORC-1" };
        var first = service.GetOrCreateToken(quote);

        var expiry = quote.ApprovalTokenExpiresAt!.Value;
        quote.ApprovalTokenExpiresAt = new DateTime(expiry.Ticks - expiry.Ticks % 10, expiry.Kind); // µs truncation

        service.GetOrCreateToken(quote).Should().Be(first);
    }
}
