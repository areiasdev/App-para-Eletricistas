using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;
using TecnicoApp.Application.Common.Interfaces;

namespace TecnicoApp.Infrastructure.Services;

/// <summary>
/// Creates a single Stripe Checkout Session per invoice with both "card" and "mb_way" enabled —
/// Stripe's own hosted checkout page lets the payer choose between them, so no
/// provider-per-method abstraction is needed here (unlike a storefront that lets the customer
/// pick a method upfront). Uses the self-hosted install's own Stripe secret key, configured the
/// same way SMTP already is (per-tenant config, not a shared platform account).
/// </summary>
public class StripeCheckoutService : IStripeCheckoutService
{
    public StripeCheckoutService(IConfiguration configuration)
    {
        StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];
    }

    public async Task<(string SessionId, string CheckoutUrl)> CreateSessionAsync(
        decimal amountEur,
        string description,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default)
    {
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = ["card", "mb_way"],
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "eur",
                        UnitAmount = (long)(amountEur * 100),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = description
                        }
                    }
                }
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options, cancellationToken: cancellationToken);

        return (session.Id, session.Url);
    }
}
