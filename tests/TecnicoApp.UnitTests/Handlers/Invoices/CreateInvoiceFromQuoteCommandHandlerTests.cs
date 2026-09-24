using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Invoices.Commands.CreateInvoiceFromQuote;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.Infrastructure.Persistence;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Invoices;

public class CreateInvoiceFromQuoteCommandHandlerTests
{
    private static ICurrentUserService AsUser(User user)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(user.Id);
        currentUser.Email.Returns(user.Email);
        return currentUser;
    }

    private static CreateInvoiceFromQuoteCommandHandler MakeHandler(
        AppDbContext db, ICurrentUserService currentUser)
    {
        var logger = Substitute.For<ILogger<CreateInvoiceFromQuoteCommandHandler>>();
        var appSettings = Substitute.For<IAppSettings>();
        appSettings.InvoicePaymentTermDays.Returns(30);
        return new CreateInvoiceFromQuoteCommandHandler(db, currentUser, appSettings, logger);
    }

    private static (User owner, User technician, Client client) SeedTeamWithClient(
        AppDbContext db)
    {
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner", Role = UserRole.Owner };
        var technician = new User
        {
            Email = "tech@x.pt", PasswordHash = "h", FullName = "Tech",
            OwnerId = owner.Id, Role = UserRole.Technician
        };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        db.Users.AddRange(owner, technician);
        db.Clients.Add(client);
        return (owner, technician, client);
    }

    [Fact]
    public async Task Handle_technician_is_forbidden()
    {
        using var db = TestDb.Create();
        var (owner, technician, client) = SeedTeamWithClient(db);
        var quote = new Quote
        {
            Number = "ORC-2026-0001", ClientId = client.Id, UserId = owner.Id, Status = QuoteStatus.Accepted,
            Lines = [new QuoteLine { Description = "Serviço", Quantity = 1, UnitPrice = 100m, VatRate = 23m }],
        };
        db.Quotes.Add(quote);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = MakeHandler(db, AsUser(technician));
        var result = await handler.Handle(new CreateInvoiceFromQuoteCommand(quote.Id), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
        db.Invoices.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_quote_not_accepted_is_rejected()
    {
        using var db = TestDb.Create();
        var (owner, _, client) = SeedTeamWithClient(db);
        var quote = new Quote
        {
            Number = "ORC-2026-0001", ClientId = client.Id, UserId = owner.Id, Status = QuoteStatus.Sent,
            Lines = [new QuoteLine { Description = "Serviço", Quantity = 1, UnitPrice = 100m, VatRate = 23m }],
        };
        db.Quotes.Add(quote);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = MakeHandler(db, AsUser(owner));
        var result = await handler.Handle(new CreateInvoiceFromQuoteCommand(quote.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.Error);
        db.Invoices.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_double_invoicing_is_rejected()
    {
        using var db = TestDb.Create();
        var (owner, _, client) = SeedTeamWithClient(db);
        var quote = new Quote
        {
            Number = "ORC-2026-0001", ClientId = client.Id, UserId = owner.Id, Status = QuoteStatus.Accepted,
            Lines = [new QuoteLine { Description = "Serviço", Quantity = 1, UnitPrice = 100m, VatRate = 23m }],
        };
        db.Quotes.Add(quote);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = MakeHandler(db, AsUser(owner));
        var first = await handler.Handle(new CreateInvoiceFromQuoteCommand(quote.Id), CancellationToken.None);
        first.IsSuccess.Should().BeTrue();

        // Quote status flips to Invoiced as part of creating the first invoice, but even if
        // it hadn't, the "no non-cancelled invoice already references this quote" check must
        // independently block a second attempt.
        var second = await handler.Handle(new CreateInvoiceFromQuoteCommand(quote.Id), CancellationToken.None);

        second.IsSuccess.Should().BeFalse();
        second.Status.Should().Be(Ardalis.Result.ResultStatus.Error);
        db.Invoices.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_success_snapshots_lines_and_sets_quote_invoiced()
    {
        using var db = TestDb.Create();
        var (owner, _, client) = SeedTeamWithClient(db);
        var quote = new Quote
        {
            Number = "ORC-2026-0001", ClientId = client.Id, UserId = owner.Id, Status = QuoteStatus.Accepted,
            Discount = 10m, Notes = "Nota do orçamento",
            Lines =
            [
                new QuoteLine { Description = "Serviço A", Quantity = 2, UnitPrice = 50m, VatRate = 23m },
                new QuoteLine { Description = "Serviço B", Quantity = 1, UnitPrice = 30m, VatRate = 6m },
            ],
        };
        db.Quotes.Add(quote);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = MakeHandler(db, AsUser(owner));
        var result = await handler.Handle(new CreateInvoiceFromQuoteCommand(quote.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Number.Should().EndWith("0001");
        result.Value.Number.Should().StartWith("FT-");
        result.Value.QuoteId.Should().Be(quote.Id);
        result.Value.QuoteNumber.Should().Be(quote.Number);
        result.Value.Discount.Should().Be(10m);
        result.Value.Notes.Should().Be("Nota do orçamento");
        result.Value.Lines.Should().HaveCount(2);

        var savedQuote = db.Quotes.Single(q => q.Id == quote.Id);
        savedQuote.Status.Should().Be(QuoteStatus.Invoiced, "creating the invoice must also flip the source quote to Invoiced");

        var savedInvoice = db.Invoices.Include(i => i.Lines).Single();
        savedInvoice.ClientId.Should().Be(client.Id);
        savedInvoice.UserId.Should().Be(owner.Id, "invoices are owned by the team, not the individual who created them");
        savedInvoice.Lines.Should().HaveCount(2);
        savedInvoice.DueDate.Should().Be(savedInvoice.IssuedAt.AddDays(30));
    }

    [Fact]
    public async Task Handle_number_increments_within_owner_and_year()
    {
        using var db = TestDb.Create();
        var (owner, _, client) = SeedTeamWithClient(db);
        var quoteA = new Quote
        {
            Number = "ORC-2026-0001", ClientId = client.Id, UserId = owner.Id, Status = QuoteStatus.Accepted,
            Lines = [new QuoteLine { Description = "Serviço", Quantity = 1, UnitPrice = 100m, VatRate = 23m }],
        };
        var quoteB = new Quote
        {
            Number = "ORC-2026-0002", ClientId = client.Id, UserId = owner.Id, Status = QuoteStatus.Accepted,
            Lines = [new QuoteLine { Description = "Serviço", Quantity = 1, UnitPrice = 100m, VatRate = 23m }],
        };
        db.Quotes.AddRange(quoteA, quoteB);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = MakeHandler(db, AsUser(owner));
        var first = await handler.Handle(new CreateInvoiceFromQuoteCommand(quoteA.Id), CancellationToken.None);
        var second = await handler.Handle(new CreateInvoiceFromQuoteCommand(quoteB.Id), CancellationToken.None);

        first.Value.Number.Should().EndWith("0001");
        second.Value.Number.Should().EndWith("0002");
    }
}
