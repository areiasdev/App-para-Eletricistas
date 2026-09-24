using TecnicoApp.Domain.Entities;

namespace TecnicoApp.Application.Common.Interfaces;

public interface IQuoteApprovalLinkService
{
    /// <summary>
    /// Ensures the quote has a valid approval token and returns the RAW token for
    /// "{BaseUrl}/orcamento/{token}". Same deterministic scheme as <see cref="IInvoicePayLinkService"/>:
    /// re-sending the quote keeps the link the client already has working. Mutates
    /// Quote.ApprovalTokenHash/ApprovalTokenExpiresAt in memory — the caller saves.
    /// </summary>
    string GetOrCreateToken(Quote quote);
}
