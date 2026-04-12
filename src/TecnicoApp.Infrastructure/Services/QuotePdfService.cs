using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TecnicoApp.Application.Common.Interfaces;

namespace TecnicoApp.Infrastructure.Services;

public class QuotePdfService : IPdfService
{
    private static readonly string AmberHex  = "#f59e0b";
    private static readonly string InkHex    = "#1a1a1a";
    private static readonly string MutedHex  = "#6b7280";
    private static readonly string LineHex   = "#e5e7eb";
    private static readonly string CanvasHex = "#f7f7f4";

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
                page.Footer().Element(ComposeFooter());
            });
        }).GeneratePdf();
    }

    // ── Header ────────────────────────────────────────────────────────────────
    private static Action<IContainer> ComposeHeader(QuotePdfData d) => container =>
    {
        container.PaddingBottom(24).Row(row =>
        {
            // Left: issuer info
            row.RelativeItem().Column(col =>
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

            // Right: "ORÇAMENTO" badge + number
            row.ConstantItem(160).AlignRight().Column(col =>
            {
                col.Item().Background(AmberHex).Padding(8).AlignCenter()
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
        container.Column(col =>
        {
            // Divider
            col.Item().BorderBottom(1).BorderColor(AmberHex).PaddingBottom(0);

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
                    cols.RelativeColumn(1);   // Qtd
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
                    h.Cell().Element(c => HeaderCell(c, "TOTAL"));
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
                    table.Cell().Element(c => CellRight(c, line.Quantity.ToString("G")));
                    table.Cell().Element(c => CellRight(c, line.UnitPrice.ToString("C", new System.Globalization.CultureInfo("pt-PT"))));
                    table.Cell().Element(c => CellRight(c, $"{line.VatRate}%"));
                    table.Cell().Element(c => CellRight(c, line.LineTotal.ToString("C", new System.Globalization.CultureInfo("pt-PT"))));
                }
            });

            // Totals
            var ptCulture = new System.Globalization.CultureInfo("pt-PT");
            col.Item().PaddingTop(12).AlignRight().Width(200).Column(totals =>
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

                TotalRow("Subtotal", d.SubTotal.ToString("C", ptCulture));
                TotalRow("IVA", d.VatTotal.ToString("C", ptCulture));

                if (d.Discount.HasValue && d.Discount > 0)
                    TotalRow("Desconto", $"-{d.Discount.Value.ToString("C", ptCulture)}");

                totals.Item().PaddingTop(6).BorderTop(1).BorderColor(LineHex).PaddingBottom(0);
                totals.Item().PaddingTop(6);
                TotalRow("TOTAL", d.Total.ToString("C", ptCulture), bold: true);
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
        });
    };

    // ── Footer ────────────────────────────────────────────────────────────────
    private static Action<IContainer> ComposeFooter() => container =>
    {
        container.BorderTop(1).BorderColor(LineHex).PaddingTop(8)
            .Row(row =>
            {
                row.RelativeItem().Text("Documento gerado por TécnicoApp").FontColor(MutedHex).FontSize(8);
                row.RelativeItem().AlignRight()
                    .Text(t =>
                    {
                        t.Span("Página ").FontColor(MutedHex).FontSize(8);
                        t.CurrentPageNumber().FontColor(MutedHex).FontSize(8);
                        t.Span(" de ").FontColor(MutedHex).FontSize(8);
                        t.TotalPages().FontColor(MutedHex).FontSize(8);
                    });
            });
    };
}
