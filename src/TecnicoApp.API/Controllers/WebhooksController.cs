using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.API.Controllers;

[ApiController]
[Route("api/v1/webhooks")]
public class WebhooksController(IAppDbContext db, IConfiguration configuration) : ControllerBase
{
    [HttpPost("stripe")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Stripe(CancellationToken ct)
    {
        // Raw body is required for signature verification — [FromBody] model binding would
        // parse/re-serialize the JSON and break the signature check.
        var json = await new StreamReader(Request.Body).ReadToEndAsync(ct);
        var webhookSecret = configuration["Stripe:WebhookSecret"];

        Event stripeEvent;
        try
        {
            // throwOnApiVersionMismatch: false — Stripe events carry whatever API version is
            // pinned on the merchant's own Stripe dashboard, which will essentially never match
            // whatever version this specific Stripe.net package build expects. Leaving the
            // default (true) would make EVERY genuine, correctly-signed production webhook throw
            // here and get rejected as "Invalid Stripe signature" — the signature itself would
            // never actually be the problem, but the real cause is masked by the catch below.
            stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                webhookSecret,
                throwOnApiVersionMismatch: false);
        }
        catch (StripeException)
        {
            return BadRequest("Invalid Stripe signature.");
        }

        // Card: checkout.session.completed fires with payment_status=paid immediately.
        // MB WAY: checkout.session.completed fires with payment_status=unpaid (it's an
        // async/OTP-confirmed method) — the actual confirmation arrives later via
        // checkout.session.async_payment_succeeded. Both must be handled or MB WAY payments
        // would silently never mark the invoice paid.
        if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
        {
            if (stripeEvent.Data.Object is Stripe.Checkout.Session { PaymentStatus: "paid" } session)
                await MarkInvoicePaidAsync(session.Id, ct);
        }
        else if (stripeEvent.Type == EventTypes.CheckoutSessionAsyncPaymentSucceeded)
        {
            if (stripeEvent.Data.Object is Stripe.Checkout.Session session)
                await MarkInvoicePaidAsync(session.Id, ct);
        }

        return Ok();
    }

    private async Task MarkInvoicePaidAsync(string sessionId, CancellationToken ct)
    {
        var invoice = await db.Invoices
            .FirstOrDefaultAsync(i => i.StripeCheckoutSessionId == sessionId, ct);

        // Idempotent — Stripe retries webhook deliveries, and an invoice already marked Paid
        // (e.g. by the technician manually, or a previous delivery of this same event) must not
        // be touched again.
        if (invoice is null || invoice.Status == InvoiceStatus.Paid)
            return;

        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
