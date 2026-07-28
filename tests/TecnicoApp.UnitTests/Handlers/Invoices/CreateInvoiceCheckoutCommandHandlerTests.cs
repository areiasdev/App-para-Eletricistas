using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Invoices.Commands.CreateInvoiceCheckout;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.Infrastructure.Services;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Invoices;

public class CreateInvoiceCheckoutCommandHandlerTests
{
    private static IAppSettings MakeAppSettings()
    {
        var appSettings = Substitute.For<IAppSettings>();
        appSettings.BaseUrl.Returns("http://localhost:3000");
        return appSettings;
    }

    private static (User owner, Client client, Invoice invoice) SeedInvoice(
        TecnicoApp.Infrastructure.Persistence.AppDbContext db, InvoiceStatus status = InvoiceStatus.Issued)
    {
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner", Role = UserRole.Owner };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var invoice = new Invoice
        {
            Number = "FT-2026-0001", ClientId = client.Id, UserId = owner.Id, DueDate = DateTime.UtcNow.AddDays(30),
            Status = status,
            Lines = [new InvoiceLine { Description = "Serviço", Quantity = 1, UnitPrice = 100m, VatRate = 23m }],
        };
        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Invoices.Add(invoice);
        db.SaveChanges();
        return (owner, client, invoice);
    }

    private static string IssueRawToken(TecnicoApp.Infrastructure.Persistence.AppDbContext db, Invoice invoice)
    {
        var payLinkService = new InvoicePayLinkService(FakeConfig.Create());
        var rawToken = payLinkService.GetOrCreateToken(invoice);
        db.SaveChanges();
        return rawToken;
    }

    [Fact]
    public async Task Handle_paid_invoice_is_rejected()
    {
        using var db = TestDb.Create();
        var (_, _, invoice) = SeedInvoice(db, InvoiceStatus.Paid);
        var rawToken = IssueRawToken(db, invoice);

        var stripe = Substitute.For<IStripeCheckoutService>();
        var handler = new CreateInvoiceCheckoutCommandHandler(db, stripe, MakeAppSettings());

        var result = await handler.Handle(new CreateInvoiceCheckoutCommand(rawToken), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await stripe.DidNotReceive().CreateSessionAsync(
            Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_cancelled_invoice_is_rejected()
    {
        using var db = TestDb.Create();
        var (_, _, invoice) = SeedInvoice(db, InvoiceStatus.Cancelled);
        var rawToken = IssueRawToken(db, invoice);

        var stripe = Substitute.For<IStripeCheckoutService>();
        var handler = new CreateInvoiceCheckoutCommandHandler(db, stripe, MakeAppSettings());

        var result = await handler.Handle(new CreateInvoiceCheckoutCommand(rawToken), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await stripe.DidNotReceive().CreateSessionAsync(
            Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_issued_invoice_creates_checkout_session_and_stores_session_id()
    {
        using var db = TestDb.Create();
        var (_, _, invoice) = SeedInvoice(db, InvoiceStatus.Issued);
        var rawToken = IssueRawToken(db, invoice);

        var stripe = Substitute.For<IStripeCheckoutService>();
        stripe.CreateSessionAsync(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(("cs_test_123", "https://checkout.stripe.com/cs_test_123"));

        var handler = new CreateInvoiceCheckoutCommandHandler(db, stripe, MakeAppSettings());
        var result = await handler.Handle(new CreateInvoiceCheckoutCommand(rawToken), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("https://checkout.stripe.com/cs_test_123");

        var saved = db.Invoices.Single(i => i.Id == invoice.Id);
        saved.StripeCheckoutSessionId.Should().Be("cs_test_123");

        await stripe.Received(1).CreateSessionAsync(
            invoice.Total, Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains("success=true")),
            Arg.Is<string>(s => s.Contains("cancelled=true")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_invalid_token_is_not_found()
    {
        using var db = TestDb.Create();
        SeedInvoice(db);

        var stripe = Substitute.For<IStripeCheckoutService>();
        var handler = new CreateInvoiceCheckoutCommandHandler(db, stripe, MakeAppSettings());

        var result = await handler.Handle(new CreateInvoiceCheckoutCommand("garbage-token"), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
    }
}
