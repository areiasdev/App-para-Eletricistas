using QuestPDF.Fluent;
using TecnicoApp.Application.Common.Formatting;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Domain.Common;

namespace TecnicoApp.Infrastructure.Services;

public class QuotePdfService : IPdfService
{
    private static readonly string AmberHex  = PdfStyle.AmberHex;
    private static readonly string InkHex    = PdfStyle.InkHex;
    private static readonly string MutedHex  = PdfStyle.MutedHex;
    private static readonly string LineHex   = PdfStyle.LineHex;
    private static readonly string CanvasHex = PdfStyle.CanvasHex;

    // IPdfService is registered as a single scoped service, so this class is the one
    // resolved for both document kinds — GenerateInvoicePdf just hands off to the sibling
    // service that owns the invoice-specific composition. See PdfStyle's doc comment for
    // why the composition logic itself isn't merged into one method.
    public byte[] GenerateInvoicePdf(InvoicePdfData data) => new InvoicePdfService().Generate(data);

    public byte[] GenerateInterventionReportPdf(InterventionReportPdfData data) => new InterventionReportPdfService().Generate(data);

    public byte[] GenerateQuotePdf(QuotePdfData d)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(10).FontColor(InkHex));

                page.Header().Element(ComposeHeader(d));
                page.Content().Element(ComposeContent(d));
                page.Footer().Element(PdfStyle.ComposeFooter(d.IssuerCompany ?? d.IssuerName));
            });
        }).GeneratePdf();
    }

    // ── Header ────────────────────────────────────────────────────────────────
    private static Action<IContainer> ComposeHeader(QuotePdfData d) => container =>
    {
        var brandHex = string.IsNullOrWhiteSpace(d.IssuerBrandColorHex) ? AmberHex : d.IssuerBrandColorHex;

        container.PaddingBottom(24).Row(row =>
        {
            // Left: logo (if set) + issuer info
            row.RelativeItem().Row(inner =>
            {
                if (d.IssuerLogoBytes is { Length: > 0 })
                {
                    inner.ConstantItem(48).Height(48).AlignMiddle()
                        .Image(d.IssuerLogoBytes).FitArea();
                    inner.ConstantItem(12);
                }

                inner.RelativeItem().Column(col =>
                {
                    col.Item().Text(d.IssuerCompany ?? d.IssuerName)
                        .FontSize(16).Bold().FontColor(InkHex);

                    if (d.IssuerCompany is not null)
                        col.Item().Text(d.IssuerName).FontSize(10).FontColor(MutedHex);

                    col.Item().PaddingTop(4).Text(t =>
                    {
                        if (d.IssuerNif is not null)
                        {
                            t.Span("NIF: ").FontColor(MutedHex);
                            t.Span(d.IssuerNif);
                            t.Span("   ");
                        }
                        if (d.IssuerPhone is not null)
                        {
                            t.Span(d.IssuerPhone).FontColor(MutedHex);
                        }
                    });

                    if (d.IssuerEmail is not null)
                        col.Item().Text(d.IssuerEmail).FontColor(MutedHex);
                });
            });

            // Right: "ORÇAMENTO" badge + number
            row.ConstantItem(160).AlignRight().Column(col =>
            {
                col.Item().Background(brandHex).Padding(8).AlignCenter()
                    .Text("ORÇAMENTO").Bold().FontSize(13).FontColor("#ffffff");

                col.Item().PaddingTop(6).AlignRight()
                    .Text(d.Number).Bold().FontSize(14).FontColor(InkHex);

                col.Item().AlignRight()
                    .Text($"Data: {d.CreatedAt:dd/MM/yyyy}").FontColor(MutedHex);

                if (d.ValidUntil.HasValue)
                    col.Item().AlignRight()
                        .Text($"Válido até: {d.ValidUntil:dd/MM/yyyy}").FontColor(MutedHex);
            });
        });
    };

    // ── Content ───────────────────────────────────────────────────────────────
    private static Action<IContainer> ComposeContent(QuotePdfData d) => container =>
    {
        var brandHex = string.IsNullOrWhiteSpace(d.IssuerBrandColorHex) ? AmberHex : d.IssuerBrandColorHex;

        container.Column(col =>
        {
            // Divider
            col.Item().BorderBottom(1).BorderColor(brandHex).PaddingBottom(0);

            // Client block
            col.Item().PaddingTop(20).PaddingBottom(20).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("CLIENTE").FontSize(8).Bold()
                        .LetterSpacing(0.08f).FontColor(MutedHex);
                    c.Item().PaddingTop(4).Text(d.ClientName).Bold().FontSize(12);
                    if (d.ClientNif is not null)
                        c.Item().Text($"NIF: {d.ClientNif}").FontColor(MutedHex);
                    if (d.ClientAddress is not null)
                        c.Item().Text(d.ClientAddress).FontColor(MutedHex);
                    if (d.ClientEmail is not null)
                        c.Item().Text(d.ClientEmail).FontColor(MutedHex);
                    if (d.ClientPhone is not null)
                        c.Item().Text(d.ClientPhone).FontColor(MutedHex);
                });
            });

            // Lines table
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(5);   // Descrição
                    cols.RelativeColumn(1.4f); // Qtd + unidade
                    cols.RelativeColumn(2);   // Preço unit.
                    cols.RelativeColumn(1);   // IVA
                    cols.RelativeColumn(2);   // Total
                });

                // Header row
                static void HeaderCell(IContainer c, string text) =>
                    c.Background(CanvasHex).Padding(8)
                     .Text(text).FontSize(8).Bold().LetterSpacing(0.06f).FontColor(MutedHex);

                table.Header(h =>
                {
                    h.Cell().Element(c => HeaderCell(c, "DESCRIÇÃO"));
                    h.Cell().Element(c => HeaderCell(c, "QTD"));
                    h.Cell().Element(c => HeaderCell(c, "PREÇO UNIT."));
                    h.Cell().Element(c => HeaderCell(c, "IVA"));
                    h.Cell().Element(c => HeaderCell(c, "VALOR S/ IVA"));
                });

                // Data rows
                foreach (var line in d.Lines)
                {
                    static void Cell(IContainer c, string text, bool right = false) =>
                        c.BorderBottom(1).BorderColor(LineHex).Padding(8)
                         .AlignLeft().Text(text);

                    static void CellRight(IContainer c, string text) =>
                        c.BorderBottom(1).BorderColor(LineHex).Padding(8)
                         .AlignRight().Text(text);

                    table.Cell().Element(c => Cell(c, line.Description));
                    table.Cell().Element(c => CellRight(c, PdfStyle.FormatQuantity(line.Quantity, line.Unit)));
                    table.Cell().Element(c => CellRight(c, PtFormat.UnitPrice(line.UnitPrice)));
                    table.Cell().Element(c => CellRight(c, PtFormat.Percent(line.VatRate)));
                    table.Cell().Element(c => CellRight(c, PtFormat.Currency(line.LineNet())));
                }
            });

            // Totals
            col.Item().PaddingTop(12).AlignRight().Width(260).Column(totals =>
            {
                void TotalRow(string label, string value, bool bold = false)
                {
                    totals.Item().Row(r =>
                    {
                        if (bold)
                        {
                            r.RelativeItem().Text(label).Bold().FontColor(InkHex);
                            r.ConstantItem(80).AlignRight().Text(value).Bold().FontColor(InkHex);
                        }
                        else
                        {
                            r.RelativeItem().Text(label).FontColor(MutedHex);
                            r.ConstantItem(80).AlignRight().Text(value).FontColor(MutedHex);
                        }
                    });
                }

                TotalRow("Subtotal", PtFormat.Currency(d.SubTotal));
                foreach (var (rate, taxableBase, vat) in DocumentMath.VatBreakdown(d.Lines))
                    TotalRow($"IVA {PtFormat.Percent(rate)} s/ {PtFormat.Currency(taxableBase)}", PtFormat.Currency(vat));

                if (d.Discount.HasValue && d.Discount > 0)
                    TotalRow("Desconto", $"-{PtFormat.Currency(d.Discount.Value)}");

                totals.Item().PaddingTop(6).BorderTop(1).BorderColor(LineHex).PaddingBottom(0);
                totals.Item().PaddingTop(6);
                TotalRow("TOTAL", PtFormat.Currency(d.Total), bold: true);
            });

            // Notes
            if (!string.IsNullOrWhiteSpace(d.Notes))
            {
                col.Item().PaddingTop(24).Column(n =>
                {
                    n.Item().Text("NOTAS").FontSize(8).Bold()
                        .LetterSpacing(0.08f).FontColor(MutedHex);
                    n.Item().PaddingTop(4).Background(CanvasHex).Padding(12)
                        .Text(d.Notes).FontColor(InkHex);
                });
            }

            // Client acceptance (signed on the technician's device or online via the approval link)
            if (d.SignatureImage is { Length: > 0 })
            {
                col.Item().PaddingTop(24).ShowEntire().Width(240).Column(a =>
                {
                    a.Item().Text("ACEITE PELO CLIENTE").FontSize(8).Bold()
                        .LetterSpacing(0.08f).FontColor(MutedHex);
                    a.Item().PaddingTop(4).Height(60).BorderBottom(1).BorderColor(LineHex)
                        .Image(d.SignatureImage).FitArea();
                    var signedBy = string.Join(" — ", new[] { d.SignedByName, d.SignedAt is { } at ? PtFormat.DateTime(at) : null }
                        .Where(x => !string.IsNullOrWhiteSpace(x)));
                    if (signedBy.Length > 0)
                        a.Item().PaddingTop(4).Text(signedBy).FontColor(MutedHex);
                });
            }
        });
    };

}
