using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Common.Security;
using TecnicoApp.Domain.Entities;

namespace TecnicoApp.Infrastructure.Services;

/// <summary>
/// Only Invoice.PayTokenHash (a SHA256 hash) is persisted — the raw token itself is never
/// stored, mirroring ClientPortalController's magic-link technique. That alone would make it
/// impossible to "reuse" (return the same working link) on a second call, since a one-way hash
/// can't be reversed. Instead, the raw token is derived *deterministically* via HMAC-SHA256 over
/// the invoice id and its (already persisted) PayTokenExpiresAt — so as long as those two values
/// haven't changed, recomputing the raw token reproduces the exact same value, and therefore the
/// exact same hash, without ever storing the raw token anywhere.
/// </summary>
public class InvoicePayLinkService(IConfiguration configuration) : IInvoicePayLinkService
{
    // Derived from (not equal to) the JWT signing secret via a one-way SHA256 pass with a fixed
    // label — key separation: a leak of this derived key doesn't hand over the JWT secret
    // itself, and vice versa a JWT-secret leak still requires this same derivation step rather
    // than being directly reusable. The JWT secret is already validated at startup to be a
    // strong random string (see Program.cs).
    private readonly byte[] _hmacKey = DerivedLinkToken.DeriveKey(configuration, "invoice-pay-link-key");

    public string GetOrCreateToken(Invoice invoice)
    {
        var isValid = !string.IsNullOrEmpty(invoice.PayTokenHash)
            && invoice.PayTokenExpiresAt.HasValue
            && invoice.PayTokenExpiresAt.Value > DateTime.UtcNow;

        if (!isValid)
        {
            // An invoice can legitimately stay unpaid for months, and this token is
            // single-purpose (view+pay one invoice, not full account access) — a long expiry
            // is low-risk and avoids the link going stale before the invoice does.
            invoice.PayTokenExpiresAt = DerivedLinkToken.WholeSeconds(DateTime.UtcNow.AddYears(1));
        }

        var rawToken = DerivedLinkToken.Derive(_hmacKey, "invoice-pay", invoice.Id, invoice.PayTokenExpiresAt!.Value);
        invoice.PayTokenHash = PublicTokens.Hash(rawToken);
        return rawToken;
    }
}

/// <summary>
/// Deterministic link tokens: HMAC-SHA256 over (purpose, entity id, expiry) with a key derived
/// from the JWT secret. Recomputing with the same inputs reproduces the same token, so a link can
/// be "re-issued" without ever storing the raw value — only its hash is persisted.
/// </summary>
internal static class DerivedLinkToken
{
    public static byte[] DeriveKey(IConfiguration configuration, string label) =>
        SHA256.HashData(Encoding.UTF8.GetBytes($"{label}:{configuration["Jwt:Secret"]}"));

    /// <summary>
    /// The token is derived from the expiry's ticks, so the expiry must survive a database
    /// round-trip unchanged. PostgreSQL keeps microseconds, .NET ticks are 100 ns — an untruncated
    /// value comes back different, the re-derived token no longer matches, and every link already
    /// emailed silently stops working the next time the document is re-sent.
    /// </summary>
    public static DateTime WholeSeconds(DateTime value) =>
        new(value.Ticks - value.Ticks % TimeSpan.TicksPerSecond, value.Kind);

    public static string Derive(byte[] key, string purpose, Guid entityId, DateTime expiresAt)
    {
        using var hmac = new HMACSHA256(key);
        var raw = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{purpose}:{entityId:N}:{expiresAt.Ticks}"));
        return Convert.ToBase64String(raw).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}
