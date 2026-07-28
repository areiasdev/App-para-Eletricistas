using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using TecnicoApp.Application.Common.Interfaces;
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
    private readonly byte[] _hmacKey = SHA256.HashData(
        Encoding.UTF8.GetBytes($"invoice-pay-link-key:{configuration["Jwt:Secret"]}"));

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
            invoice.PayTokenExpiresAt = DateTime.UtcNow.AddYears(1);
        }

        var rawToken = DeriveToken(invoice.Id, invoice.PayTokenExpiresAt!.Value);
        invoice.PayTokenHash = Hash(rawToken);
        return rawToken;
    }

    private string DeriveToken(Guid invoiceId, DateTime expiresAt)
    {
        using var hmac = new HMACSHA256(_hmacKey);
        var input = Encoding.UTF8.GetBytes($"invoice-pay:{invoiceId:N}:{expiresAt.Ticks}");
        var raw = hmac.ComputeHash(input);
        return Convert.ToBase64String(raw).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
