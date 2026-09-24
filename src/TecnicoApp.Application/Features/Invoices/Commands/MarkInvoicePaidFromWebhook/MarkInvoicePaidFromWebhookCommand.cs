using MediatR;

namespace TecnicoApp.Application.Features.Invoices.Commands.MarkInvoicePaidFromWebhook;

// No Result<> wrapper — the webhook controller always returns 200 to Stripe regardless of
// outcome (an unknown session id or an already-Paid invoice are not errors Stripe should
// retry over), so there's nothing for the caller to branch on.
// InvoiceId comes from the session's client_reference_id. It's the fallback when the session id
// no longer matches: every "Pagar agora" click creates a new session and overwrites
// Invoice.StripeCheckoutSessionId, so a customer who opened checkout twice and paid in the first
// tab would otherwise have a real payment that never marks the invoice paid.
public record MarkInvoicePaidFromWebhookCommand(string StripeCheckoutSessionId, Guid? InvoiceId = null) : IRequest;
