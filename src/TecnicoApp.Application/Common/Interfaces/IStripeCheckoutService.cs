namespace TecnicoApp.Application.Common.Interfaces;

public interface IStripeCheckoutService
{
    /// <summary>
    /// Creates a single Stripe Checkout Session with both "card" and "mb_way" payment method
    /// types enabled — Stripe's own hosted checkout page lets the payer pick between them.
    /// Uses the self-hosted install's own Stripe secret key (Stripe:SecretKey config).
    /// </summary>
    /// <param name="clientReferenceId">Our own id for what is being paid (the invoice id). Stripe echoes
    /// it back on the webhook, so the payment can be matched even if a newer session replaced this one.</param>
    Task<(string SessionId, string CheckoutUrl)> CreateSessionAsync(
        decimal amountEur,
        string description,
        string successUrl,
        string cancelUrl,
        string clientReferenceId,
        CancellationToken cancellationToken = default);
}
