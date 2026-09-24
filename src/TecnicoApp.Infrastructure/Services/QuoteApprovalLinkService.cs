using Microsoft.Extensions.Configuration;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Common.Security;
using TecnicoApp.Domain.Entities;

namespace TecnicoApp.Infrastructure.Services;

/// <summary>Quote-approval counterpart of <see cref="InvoicePayLinkService"/> (same derivation scheme, own key label).</summary>
public class QuoteApprovalLinkService(IConfiguration configuration) : IQuoteApprovalLinkService
{
    /// <summary>Link lifetime when the quote has no "válido até" date.</summary>
    public static readonly TimeSpan DefaultLifetime = TimeSpan.FromDays(60);

    private readonly byte[] _hmacKey = DerivedLinkToken.DeriveKey(configuration, "quote-approval-key");

    public string GetOrCreateToken(Quote quote)
    {
        var isValid = !string.IsNullOrEmpty(quote.ApprovalTokenHash)
            && quote.ApprovalTokenExpiresAt > DateTime.UtcNow;

        if (!isValid)
        {
            // A few days' grace past "válido até" so a client answering on the last day isn't cut off;
            // accepting after the validity date is still refused by the accept command itself.
            quote.ApprovalTokenExpiresAt = DerivedLinkToken.WholeSeconds(
                quote.ValidUntil is { } validUntil && validUntil > DateTime.UtcNow
                    ? validUntil.AddDays(7)
                    : DateTime.UtcNow.Add(DefaultLifetime));
        }

        var rawToken = DerivedLinkToken.Derive(_hmacKey, "quote-approval", quote.Id, quote.ApprovalTokenExpiresAt!.Value);
        quote.ApprovalTokenHash = PublicTokens.Hash(rawToken);
        return rawToken;
    }
}
