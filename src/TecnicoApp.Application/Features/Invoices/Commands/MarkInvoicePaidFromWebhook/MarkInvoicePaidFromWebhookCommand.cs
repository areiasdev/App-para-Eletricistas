using MediatR;

namespace TecnicoApp.Application.Features.Invoices.Commands.MarkInvoicePaidFromWebhook;

// No Result<> wrapper — the webhook controller always returns 200 to Stripe regardless of
// outcome (an unknown session id or an already-Paid invoice are not errors Stripe should
// retry over), so there's nothing for the caller to branch on.
public record MarkInvoicePaidFromWebhookCommand(string StripeCheckoutSessionId) : IRequest;
