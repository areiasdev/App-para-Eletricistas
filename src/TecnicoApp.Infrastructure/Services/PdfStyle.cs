using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace TecnicoApp.Infrastructure.Services;

/// <summary>
/// Small shared surface between QuotePdfService and InvoicePdfService: the color palette,
/// brand-color resolution, and the footer both documents render identically (save for the
/// invoice's mandatory non-fiscal disclaimer). The header/content table composition is kept
/// separate per document — the two layouts diverge enough (badge text, extra bank-details
/// block, different date fields) that sharing it would mean threading a handful of "is this
/// a quote or an invoice" flags through one method, which reads worse than the ~150 lines of
/// duplication it would save.
/// </summary>
internal static class PdfStyle
{
    public const string AmberHex  = "#f59e0b";
    public const string InkHex    = "#1a1a1a";
    public const string MutedHex  = "#6b7280";
    public const string LineHex   = "#e5e7eb";
    public const string CanvasHex = "#f7f7f4";

    public static string ResolveBrandColor(string? issuerBrandColorHex) =>
        string.IsNullOrWhiteSpace(issuerBrandColorHex) ? AmberHex : issuerBrandColorHex;

    /// <param name="issuerName">Company that issued the document — the footer is the client's
    /// view of who produced it, so it names the company, not this software.</param>
    /// <param name="disclaimer">Extra small-print line rendered above the standard footer row
    /// (e.g. the invoice's "not AT-certified" notice). Null/empty for documents that don't need one.</param>
    public static Action<IContainer> ComposeFooter(string issuerName, string? disclaimer = null) => container =>
    {
        container.Column(col =>
        {
            if (!string.IsNullOrWhiteSpace(disclaimer))
            {
                col.Item().PaddingBottom(4)
                    .Text(disclaimer).FontColor(MutedHex).FontSize(7.5f).Italic();
            }

            col.Item().BorderTop(1).BorderColor(LineHex).PaddingTop(8)
                .Row(row =>
                {
                    row.RelativeItem().Text(issuerName).FontColor(MutedHex).FontSize(8);
                    row.RelativeItem().AlignRight()
                        .Text(t =>
                        {
                            t.Span("Página ").FontColor(MutedHex).FontSize(8);
                            t.CurrentPageNumber().FontColor(MutedHex).FontSize(8);
                            t.Span(" de ").FontColor(MutedHex).FontSize(8);
                            t.TotalPages().FontColor(MutedHex).FontSize(8);
                        });
                });
        });
    };
}
