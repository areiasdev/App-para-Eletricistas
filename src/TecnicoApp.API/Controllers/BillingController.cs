using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Stripe;
using TecnicoApp.Application.Common.Interfaces;
using AppPlan = TecnicoApp.Domain.Enums.Plan;
using TecnicoApp.Infrastructure.Persistence;

namespace TecnicoApp.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class BillingController(
    IStripeService stripeService,
    ICurrentUserService currentUser,
    AppDbContext db,
    IConfiguration configuration) : ControllerBase
{
    private static readonly Dictionary<string, Plan> PlanByPriceId = [];

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(BillingMeResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null) return Unauthorized();

        return Ok(new BillingMeResponse(
            user.Plan.ToString(),
            user.StripeCustomerId is not null));
    }

    [HttpPost("checkout")]
    [Authorize]
    [ProducesResponseType(typeof(CheckoutResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateCheckout(
        [FromBody] CreateCheckoutRequest request,
        CancellationToken ct)
    {
        var userId = currentUser.UserId;

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null) return Unauthorized();

        var priceId = request.Plan switch
        {
            "Pro"  => configuration["Stripe:PriceIdPro"],
            "Team" => configuration["Stripe:PriceIdTeam"],
            _      => null,
        };

        if (string.IsNullOrWhiteSpace(priceId))
            return BadRequest(new { detail = "Plano inválido." });

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var successUrl = $"{request.FrontendUrl}/dashboard/planos?success=true";
        var cancelUrl  = $"{request.FrontendUrl}/dashboard/planos";

        var url = await stripeService.CreateCheckoutSessionAsync(
            userId.ToString(), user.Email, priceId, successUrl, cancelUrl, ct);

        return Ok(new CheckoutResponse(url));
    }

    [HttpPost("portal")]
    [Authorize]
    [ProducesResponseType(typeof(CheckoutResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreatePortal(
        [FromBody] PortalRequest request,
        CancellationToken ct)
    {
        var userId = currentUser.UserId;

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(user.StripeCustomerId))
            return BadRequest(new { detail = "Sem subscrição activa." });

        var returnUrl = $"{request.FrontendUrl}/dashboard/planos";
        var url = await stripeService.CreatePortalSessionAsync(user.StripeCustomerId, returnUrl, ct);
        return Ok(new CheckoutResponse(url));
    }

    /// <summary>
    /// Stripe webhook — no [Authorize], validated via webhook secret.
    /// Handles: checkout.session.completed, customer.subscription.deleted
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        var webhookSecret = configuration["Stripe:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(webhookSecret))
            return BadRequest("Webhook secret not configured.");

        string json;
        using (var reader = new StreamReader(HttpContext.Request.Body))
            json = await reader.ReadToEndAsync();

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                webhookSecret);
        }
        catch (StripeException)
        {
            return BadRequest();
        }

        switch (stripeEvent.Type)
        {
            case EventTypes.CheckoutSessionCompleted:
            {
                var session = (Stripe.Checkout.Session)stripeEvent.Data.Object;
                if (session.Mode != "subscription") break;

                var userIdStr = session.Metadata?.GetValueOrDefault("userId");
                if (!Guid.TryParse(userIdStr, out var userId)) break;

                var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user is null) break;

                // Determine plan from the price ID on the subscription
                var subscriptionService = new SubscriptionService();
                var subscription = await subscriptionService.GetAsync(session.SubscriptionId);
                var priceId = subscription.Items.Data.FirstOrDefault()?.Price.Id;

                var plan = priceId switch
                {
                    var p when p == configuration["Stripe:PriceIdTeam"] => AppPlan.Team,
                    var p when p == configuration["Stripe:PriceIdPro"]  => AppPlan.Pro,
                    _ => AppPlan.Pro
                };

                user.Plan = plan;
                user.StripeCustomerId = session.CustomerId;
                await db.SaveChangesAsync();
                break;
            }

            case EventTypes.CustomerSubscriptionDeleted:
            {
                var subscription = (Stripe.Subscription)stripeEvent.Data.Object;
                var customerId = subscription.CustomerId;

                var user = await db.Users.FirstOrDefaultAsync(u => u.StripeCustomerId == customerId);
                if (user is null) break;

                user.Plan = AppPlan.Free;
                await db.SaveChangesAsync();
                break;
            }
        }

        return Ok();
    }
}

public record BillingMeResponse(string Plan, bool HasActiveSubscription);
public record CreateCheckoutRequest(string Plan, string FrontendUrl);
public record PortalRequest(string FrontendUrl);
public record CheckoutResponse(string Url);
