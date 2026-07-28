using TecnicoApp.Domain.Entities;

namespace TecnicoApp.Application.Common.Interfaces;

public interface IInvoicePayLinkService
{
    /// <summary>
    /// Ensures the invoice has a valid (non-expired) pay token and returns the RAW token to
    /// embed in a URL as "{BaseUrl}/pay/{token}". Only the token's SHA256 hash is ever persisted
    /// (Invoice.PayTokenHash) — never the raw value — so a repeat call for an invoice that
    /// already has a non-expired token must deterministically re-derive the *same* raw token
    /// rather than rotating it; otherwise a link already copied/emailed to the client would
    /// silently stop working. Mutates Invoice.PayTokenHash/PayTokenExpiresAt in memory — the
    /// caller is responsible for calling SaveChangesAsync.
    /// </summary>
    string GetOrCreateToken(Invoice invoice);
}
