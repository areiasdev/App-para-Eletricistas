using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;

namespace TecnicoApp.Application.Common.Extensions;

public static class DocumentNumberExtensions
{
    public const string QuotePrefix = "ORC";
    public const string InvoicePrefix = "FT";

    /// <summary>
    /// Formats a sequential per-tenant, per-year document number, e.g. "ORC-2026-0004" for
    /// <c>FormatDocumentNumber("ORC", 2026, 3)</c> (the 4th quote/invoice issued that year —
    /// <paramref name="countThisYear"/> is the count of documents already issued).
    /// Shared by quote ("ORC") and invoice ("FT") numbering so the two can't drift apart
    /// (e.g. one changing zero-padding width without the other).
    /// </summary>
    public static string FormatDocumentNumber(string prefix, int year, int countThisYear) =>
        $"{prefix}-{year}-{(countThisYear + 1):D4}";

    /// <summary>Next quote number for this tenant and year, e.g. "ORC-2026-0005".</summary>
    public static async Task<string> NextQuoteNumberAsync(
        this IAppDbContext db, Guid ownerId, int year, CancellationToken cancellationToken)
    {
        // IgnoreQueryFilters: a soft-deleted draft keeps its number in the unique index, so it
        // must still be counted — otherwise the next quote is handed a number that's taken and
        // the insert fails on IX_Quotes_Number every time (the retry recounts the same value).
        var count = await db.Quotes
            .IgnoreQueryFilters()
            .CountAsync(q => q.UserId == ownerId && q.CreatedAt.Year == year, cancellationToken);
        return FormatDocumentNumber(QuotePrefix, year, count);
    }

    /// <summary>Next invoice number for this tenant and year, e.g. "FT-2026-0012".</summary>
    public static async Task<string> NextInvoiceNumberAsync(
        this IAppDbContext db, Guid ownerId, int year, CancellationToken cancellationToken)
    {
        var count = await db.Invoices
            .IgnoreQueryFilters()
            .CountAsync(i => i.UserId == ownerId && i.CreatedAt.Year == year, cancellationToken);
        return FormatDocumentNumber(InvoicePrefix, year, count);
    }
}
