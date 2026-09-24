using FluentAssertions;
using TecnicoApp.Application.Features.Invoices.Commands.MarkInvoicePaidFromWebhook;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Invoices;

public class MarkInvoicePaidFromWebhookCommandHandlerTests
{
    private static (User owner, Client client, Invoice invoice) SeedInvoice(
        TecnicoApp.Infrastructure.Persistence.AppDbContext db,
        InvoiceStatus status = InvoiceStatus.Issued,
        string sessionId = "cs_test_123")
    {
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner", Role = UserRole.Owner };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var invoice = new Invoice
        {
            Number = "FT-2026-0001", ClientId = client.Id, UserId = owner.Id, Status = status,
            IssuedAt = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(30),
            StripeCheckoutSessionId = sessionId,
        };
        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Invoices.Add(invoice);
        return (owner, client, invoice);
    }

    [Fact]
    public async Task Handle_marks_issued_invoice_paid()
    {
        using var db = TestDb.Create();
        var (_, _, invoice) = SeedInvoice(db);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new MarkInvoicePaidFromWebhookCommandHandler(db);
        await handler.Handle(new MarkInvoicePaidFromWebhookCommand("cs_test_123"), CancellationToken.None);

        var saved = db.Invoices.Single();
        saved.Status.Should().Be(InvoiceStatus.Paid);
        saved.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_replayed_webhook_delivery_is_idempotent()
    {
        using var db = TestDb.Create();
        var (_, _, invoice) = SeedInvoice(db);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new MarkInvoicePaidFromWebhookCommandHandler(db);
        await handler.Handle(new MarkInvoicePaidFromWebhookCommand("cs_test_123"), CancellationToken.None);
        var firstPaidAt = db.Invoices.Single().PaidAt;

        // Stripe retries webhook deliveries — a second delivery of the same (or a different)
        // event for an already-Paid invoice must be a no-op, not re-stamp PaidAt.
        await handler.Handle(new MarkInvoicePaidFromWebhookCommand("cs_test_123"), CancellationToken.None);

        var saved = db.Invoices.Single();
        saved.Status.Should().Be(InvoiceStatus.Paid);
        saved.PaidAt.Should().Be(firstPaidAt, "a replayed delivery must not touch an already-Paid invoice");
    }

    [Fact]
    public async Task Handle_unknown_session_id_is_a_no_op()
    {
        using var db = TestDb.Create();
        var (_, _, invoice) = SeedInvoice(db);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new MarkInvoicePaidFromWebhookCommandHandler(db);
        await handler.Handle(new MarkInvoicePaidFromWebhookCommand("cs_test_does_not_exist"), CancellationToken.None);

        db.Invoices.Single().Status.Should().Be(InvoiceStatus.Issued);
    }

    [Fact]
    public async Task Handle_cancelled_invoice_is_not_reopened_as_paid()
    {
        // Cancelled -> Paid isn't a valid transition via UpdateInvoiceStatusCommand, and the
        // webhook path must respect the same rule — only a currently-unpaid, non-Paid invoice
        // should be flipped. (Guards against a stale checkout session outliving a cancellation.)
        using var db = TestDb.Create();
        var (_, _, invoice) = SeedInvoice(db, InvoiceStatus.Cancelled);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new MarkInvoicePaidFromWebhookCommandHandler(db);
        await handler.Handle(new MarkInvoicePaidFromWebhookCommand("cs_test_123"), CancellationToken.None);

        db.Invoices.Single().Status.Should().Be(InvoiceStatus.Cancelled);
    }

    [Fact]
    public async Task Handle_superseded_session_still_marks_invoice_paid_via_client_reference_id()
    {
        // Customer opened checkout twice: the invoice now stores the second session's id, but
        // they paid in the first tab. The payment must still land on the invoice.
        using var db = TestDb.Create();
        var (_, _, invoice) = SeedInvoice(db);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new MarkInvoicePaidFromWebhookCommandHandler(db);
        await handler.Handle(new MarkInvoicePaidFromWebhookCommand("cs_test_older_session", invoice.Id), CancellationToken.None);

        var saved = db.Invoices.Single();
        saved.Status.Should().Be(InvoiceStatus.Paid);
        saved.PaidAt.Should().NotBeNull();
    }
}
