using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Invoices.Commands.MarkInvoicePaidFromWebhook;

public class MarkInvoicePaidFromWebhookCommandHandler(IAppDbContext db)
    : IRequestHandler<MarkInvoicePaidFromWebhookCommand>
{
    public async Task Handle(MarkInvoicePaidFromWebhookCommand request, CancellationToken cancellationToken)
    {
        var invoice = await db.Invoices
            .FirstOrDefaultAsync(i => i.StripeCheckoutSessionId == request.StripeCheckoutSessionId, cancellationToken);

        // Idempotent — Stripe retries webhook deliveries, so an invoice already marked Paid must
        // not be touched again. Also guard Cancelled: a technician can cancel an invoice after
        // the customer has already started (but not finished) a Stripe Checkout session — if
        // that abandoned session is later completed anyway, a stale webhook shouldn't be able to
        // silently reopen a cancelled invoice as Paid.
        if (invoice is null || invoice.Status is InvoiceStatus.Paid or InvoiceStatus.Cancelled)
            return;

        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }
}
