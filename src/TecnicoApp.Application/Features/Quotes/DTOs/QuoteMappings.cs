using TecnicoApp.Domain.Common;
using TecnicoApp.Domain.Entities;

namespace TecnicoApp.Application.Features.Quotes.DTOs;

public static class QuoteMappings
{
    /// <summary>Lines in the order they were entered, with VAT-inclusive totals from <see cref="DocumentMath"/>.</summary>
    public static IReadOnlyList<QuoteLineDto> ToLineDtos(this IEnumerable<QuoteLine> lines) =>
        lines
            .OrderBy(l => l.Position)
            .ThenBy(l => l.CreatedAt)
            .Select(l => new QuoteLineDto(
                l.Id, l.Description, l.Quantity, l.UnitPrice, l.VatRate, l.LineTotalWithVat(), l.Unit))
            .ToList();

    /// <summary>Requires <see cref="Quote.Lines"/> to be loaded; <paramref name="clientName"/> defaults to the loaded client.</summary>
    public static QuoteDto ToDto(this Quote quote, string? clientName = null) =>
        new(
            quote.Id,
            quote.Number,
            quote.Status,
            quote.Discount,
            quote.Notes,
            quote.ValidUntil,
            quote.SignedAt,
            quote.PdfUrl,
            quote.ClientId,
            clientName ?? quote.Client.Name,
            quote.SubTotal,
            quote.VatTotal,
            quote.Total,
            quote.Lines.ToLineDtos(),
            quote.CreatedAt,
            quote.EmailSentAt,
            quote.SignatureUrl,
            quote.ClientDecisionAt,
            quote.AcceptedByName,
            quote.RejectionReason);
}
