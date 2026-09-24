using System.Security.Cryptography;
using System.Text;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Invoices.Commands.CreateInvoiceCheckout;

public class CreateInvoiceCheckoutCommandHandler(
    IAppDbContext db,
    IStripeCheckoutService stripeCheckoutService,
    IAppSettings appSettings)
    : IRequestHandler<CreateInvoiceCheckoutCommand, Result<string>>
{
    public async Task<Result<string>> Handle(
        CreateInvoiceCheckoutCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return Result.NotFound();

        var tokenHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));

        var invoice = await db.Invoices
            .Include(i => i.Lines)
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.PayTokenHash == tokenHash, cancellationToken);

        if (invoice is null || invoice.PayTokenExpiresAt is null || invoice.PayTokenExpiresAt < DateTime.UtcNow)
            return Result.NotFound();

        // Nothing left to pay.
        if (invoice.Status is InvoiceStatus.Paid or InvoiceStatus.Cancelled)
            return Result.Error("Esta fatura já não pode ser paga.");

        var successUrl = $"{appSettings.BaseUrl}/pay/{request.Token}?success=true";
        var cancelUrl = $"{appSettings.BaseUrl}/pay/{request.Token}?cancelled=true";
        var description = $"Fatura {invoice.Number} — {invoice.User.CompanyName ?? invoice.User.FullName}";

        var (sessionId, checkoutUrl) = await stripeCheckoutService.CreateSessionAsync(
            invoice.Total, description, successUrl, cancelUrl, invoice.Id.ToString(), cancellationToken);

        invoice.StripeCheckoutSessionId = sessionId;
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(checkoutUrl);
    }
}
