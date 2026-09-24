namespace TecnicoApp.Domain.Common;

/// <summary>A priced line on a commercial document (quote or invoice).</summary>
public interface IDocumentLine
{
    decimal Quantity { get; }
    decimal UnitPrice { get; }
    decimal VatRate { get; }
}

/// <summary>
/// The one place that knows how document amounts are rounded and summed, shared by
/// <see cref="Entities.Quote"/>, <see cref="Entities.Invoice"/> and every DTO/PDF/email that
/// shows a line total — so a quote, the invoice created from it and its PDF can't disagree by a cent.
/// Each line is rounded individually before summing to avoid cent-level drift across many lines.
/// </summary>
public static class DocumentMath
{
    /// <summary>Standard Portuguese VAT rate (continente). Default for new lines.</summary>
    public const decimal StandardVatRate = 23m;

    /// <summary>Default unit of measure for a new line ("unidade").</summary>
    public const string DefaultUnit = "un";

    public static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static decimal LineNet(this IDocumentLine line) => Round(line.Quantity * line.UnitPrice);

    public static decimal LineVat(this IDocumentLine line) => Round(line.Quantity * line.UnitPrice * (line.VatRate / 100));

    public static decimal LineTotalWithVat(this IDocumentLine line) =>
        Round(line.Quantity * line.UnitPrice * (1 + line.VatRate / 100));

    public static decimal SubTotal(IEnumerable<IDocumentLine> lines) => lines.Sum(LineNet);

    public static decimal VatTotal(IEnumerable<IDocumentLine> lines) => lines.Sum(LineVat);

    public static decimal Total(IEnumerable<IDocumentLine> lines, decimal? discount)
    {
        var materialized = lines as IReadOnlyCollection<IDocumentLine> ?? lines.ToList();
        return Round(SubTotal(materialized) + VatTotal(materialized) - (discount ?? 0));
    }

    /// <summary>
    /// VAT summary per rate (art. 36.º CIVA: an invoice must show, for each rate, the taxable
    /// amount and the tax). Same per-line rounding as <see cref="VatTotal"/>, so the rows add up
    /// exactly to the document's VAT total.
    /// </summary>
    public static IReadOnlyList<(decimal Rate, decimal TaxableBase, decimal Vat)> VatBreakdown(IEnumerable<IDocumentLine> lines) =>
        lines
            .GroupBy(l => l.VatRate)
            .OrderByDescending(g => g.Key)
            .Select(g => (g.Key, g.Sum(LineNet), g.Sum(LineVat)))
            .ToList();
}
