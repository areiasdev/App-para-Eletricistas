namespace TecnicoApp.Application.Common.Interfaces;

public interface IStripeService
{
    /// <summary>Creates a Stripe Checkout Session and returns the URL to redirect the user to.</summary>
    Task<string> CreateCheckoutSessionAsync(
        string userId,
        string userEmail,
        string priceId,
        string successUrl,
        string cancelUrl,
        CancellationToken ct = default);

    /// <summary>Creates a Stripe Customer Portal session and returns the URL.</summary>
    Task<string> CreatePortalSessionAsync(
        string stripeCustomerId,
        string returnUrl,
        CancellationToken ct = default);
}
