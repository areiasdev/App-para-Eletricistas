namespace TecnicoApp.Application.Common.Interfaces;

public interface IStripeCheckoutService
{
    /// <summary>
    /// Creates a single Stripe Checkout Session with both "card" and "mb_way" payment method
    /// types enabled — Stripe's own hosted checkout page lets the payer pick between them.
    /// Uses the self-hosted install's own Stripe secret key (Stripe:SecretKey config).
    /// </summary>
    Task<(string SessionId, string CheckoutUrl)> CreateSessionAsync(
        decimal amountEur,
        string description,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default);
}
