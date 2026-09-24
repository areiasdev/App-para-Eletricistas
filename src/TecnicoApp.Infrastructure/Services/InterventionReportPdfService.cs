using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TecnicoApp.Application.Common.Formatting;
using TecnicoApp.Application.Common.Interfaces;

namespace TecnicoApp.Infrastructure.Services;

/// <summary>
/// Composes the job sheet (folha de obra). Like InvoicePdfService, it's reached through
/// QuotePdfService, the single IPdfService implementation.
/// </summary>
public class InterventionReportPdfService
{
    private const string InkHex = PdfStyle.InkHex;
    private const string MutedHex = PdfStyle.MutedHex;
    private const string LineHex = PdfStyle.LineHex;
    private const string CanvasHex = PdfStyle.CanvasHex;

    public byte[] Generate(InterventionReportPdfData d)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var brandHex = PdfStyle.ResolveBrandColor(d.IssuerBrandColorHex);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(10).FontColor(InkHex));

                page.Header().Element(c => ComposeHeader(c, d, brandHex));
                page.Content().Element(c => ComposeContent(c, d, brandHex));
                page.Footer().Element(PdfStyle.ComposeFooter(d.IssuerCompany ?? d.IssuerName));
            });
        }).GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, InterventionReportPdfData d, string brandHex)
    {
        container.PaddingBottom(20).Row(row =>
        {
            row.RelativeItem().Row(inner =>
            {
                if (d.IssuerLogoBytes is { Length: > 0 })
                {
                    inner.ConstantItem(48).Height(48).AlignMiddle().Image(d.IssuerLogoBytes).FitArea();
                    inner.ConstantItem(12);
                }
                inner.RelativeItem().Column(col =>
                {
                    col.Item().Text(d.IssuerCompany ?? d.IssuerName).FontSize(16).Bold();
                    var contact = string.Join("   ", new[] { d.IssuerNif is null ? null : $"NIF: {d.IssuerNif}", d.IssuerPhone, d.IssuerEmail }
                        .Where(s => !string.IsNullOrWhiteSpace(s)));
                    if (contact.Length > 0)
                        col.Item().PaddingTop(4).Text(contact).FontColor(MutedHex);
                });
            });

            row.ConstantItem(170).AlignRight().Column(col =>
            {
                col.Item().Background(brandHex).Padding(8).AlignCenter()
                    .Text("FOLHA DE OBRA").Bold().FontSize(13).FontColor("#ffffff");
                col.Item().PaddingTop(6).AlignRight().Text($"Ref. {d.Reference}").Bold().FontSize(12);
                col.Item().AlignRight().Text($"Estado: {d.Status}").FontColor(MutedHex);
            });
        });
    }

    private static void ComposeContent(IContainer container, InterventionReportPdfData d, string brandHex)
    {
        container.Column(col =>
        {
            col.Item().BorderBottom(1).BorderColor(brandHex);

            // Client + visit details
            col.Item().PaddingVertical(16).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    Label(c, "CLIENTE");
                    c.Item().PaddingTop(4).Text(d.ClientName).Bold().FontSize(12);
                    foreach (var line in new[] { d.ClientNif is null ? null : $"NIF: {d.ClientNif}", d.ClientAddress, d.ClientPhone })
                        if (!string.IsNullOrWhiteSpace(line)) c.Item().Text(line).FontColor(MutedHex);
                });
                row.RelativeItem().Column(c =>
                {
                    Label(c, "INTERVENÇÃO");
                    if (d.ScheduledAt is { } scheduled) c.Item().PaddingTop(4).Text($"Agendada: {PtFormat.DateTime(scheduled)}");
                    if (d.CompletedAt is { } completed) c.Item().Text($"Concluída: {PtFormat.DateTime(completed)}");
                    if (d.TechnicianName is not null) c.Item().Text($"Técnico: {d.TechnicianName}");
                    if (d.LaborHours is { } hours) c.Item().Text($"Horas de trabalho: {hours.ToString("0.##", PtFormat.Culture)} h");
                });
            });

            Section(col, "TRABALHO", c =>
            {
                c.Item().Text(d.Title).Bold();
                if (!string.IsNullOrWhiteSpace(d.Description)) c.Item().PaddingTop(4).Text(d.Description);
            });

            if (d.Equipment.Count > 0)
                Section(col, "EQUIPAMENTOS", c =>
                {
                    foreach (var e in d.Equipment) c.Item().Text($"• {e}");
                });

            if (!string.IsNullOrWhiteSpace(d.TechnicianNotes))
                Section(col, "NOTAS DO TÉCNICO", c => c.Item().Background(CanvasHex).Padding(10).Text(d.TechnicianNotes));

            if (d.Materials.Count > 0)
                Section(col, "MATERIAIS APLICADOS", c => c.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols => { cols.RelativeColumn(6); cols.RelativeColumn(1); });
                    foreach (var (name, quantity) in d.Materials)
                    {
                        table.Cell().BorderBottom(1).BorderColor(LineHex).PaddingVertical(5).Text(name);
                        table.Cell().BorderBottom(1).BorderColor(LineHex).PaddingVertical(5).AlignRight()
                            .Text(quantity.ToString("0.###", PtFormat.Culture));
                    }
                }));

            if (d.Photos.Count > 0)
                Section(col, "FOTOGRAFIAS", c => c.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols => { cols.RelativeColumn(); cols.RelativeColumn(); cols.RelativeColumn(); });
                    foreach (var photo in d.Photos)
                        table.Cell().Padding(3).Height(130).Image(photo).FitArea();
                }));

            // Sign-off
            col.Item().PaddingTop(24).ShowEntire().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    Label(c, "ASSINATURA DO CLIENTE");
                    c.Item().PaddingTop(6).Height(70).BorderBottom(1).BorderColor(LineHex).Element(box =>
                    {
                        if (d.SignatureImage is { Length: > 0 }) box.Image(d.SignatureImage).FitArea();
                    });
                    if (d.SignedByName is not null)
                        c.Item().PaddingTop(4).Text($"{d.SignedByName}" + (d.SignedAt is { } at ? $" — {PtFormat.DateTime(at)}" : ""))
                            .FontColor(MutedHex);
                    c.Item().PaddingTop(2).Text("Declaro que os trabalhos descritos foram executados.").FontSize(8).FontColor(MutedHex);
                });
                row.ConstantItem(40);
                row.RelativeItem().Column(c =>
                {
                    Label(c, "O TÉCNICO");
                    c.Item().PaddingTop(6).Height(70).BorderBottom(1).BorderColor(LineHex);
                    if (d.TechnicianName is not null) c.Item().PaddingTop(4).Text(d.TechnicianName).FontColor(MutedHex);
                });
            });
        });
    }

    private static void Label(ColumnDescriptor c, string text) =>
        c.Item().Text(text).FontSize(8).Bold().LetterSpacing(0.08f).FontColor(MutedHex);

    private static void Section(ColumnDescriptor col, string title, Action<ColumnDescriptor> content) =>
        col.Item().PaddingBottom(14).Column(c =>
        {
            Label(c, title);
            c.Item().PaddingTop(4).Column(content);
        });
}
