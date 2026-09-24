using TecnicoApp.Domain.Common;
using TecnicoApp.Domain.Entities;

namespace TecnicoApp.Application.Features.Invoices.DTOs;

public static class InvoiceMappings
{
    /// <summary>Lines in the order they were entered, with VAT-inclusive totals from <see cref="DocumentMath"/>.</summary>
    public static IReadOnlyList<InvoiceLineDto> ToLineDtos(this IEnumerable<InvoiceLine> lines) =>
        lines
            .OrderBy(l => l.Position)
            .ThenBy(l => l.CreatedAt)
            .Select(l => new InvoiceLineDto(
                l.Id, l.Description, l.Quantity, l.UnitPrice, l.VatRate, l.LineTotalWithVat(), l.Unit))
            .ToList();

    /// <summary>Requires <see cref="Invoice.Lines"/> to be loaded; names default to the loaded navigations.</summary>
    public static InvoiceDto ToDto(this Invoice invoice, string? clientName = null, string? quoteNumber = null) =>
        new(
            invoice.Id,
            invoice.Number,
            invoice.Status,
            invoice.Discount,
            invoice.Notes,
            invoice.IssuedAt,
            invoice.DueDate,
            invoice.PaidAt,
            invoice.ClientId,
            clientName ?? invoice.Client.Name,
            invoice.QuoteId,
            quoteNumber ?? invoice.Quote?.Number,
            invoice.SubTotal,
            invoice.VatTotal,
            invoice.Total,
            invoice.Lines.ToLineDtos(),
            invoice.CreatedAt);
}
