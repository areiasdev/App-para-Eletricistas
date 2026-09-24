using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Invoices.DTOs;
using TecnicoApp.Application.Features.Quotes.DTOs;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.Domain.ValueObjects;

namespace TecnicoApp.Application.Common.Documents;

/// <summary>
/// Builds the PDF payloads from loaded entities, so the download endpoint and the email
/// attachment can never render a document differently (they used to be built separately and
/// drifted — the emailed quote was missing the client NIF).
/// </summary>
public static class PdfDataFactory
{
    /// <param name="quote">With Lines and Client loaded.</param>
    /// <param name="issuer">The team owner — the company identity on the document.</param>
    public static QuotePdfData ForQuote(Quote quote, User issuer, byte[]? logoBytes) => new(
        Number: quote.Number,
        CreatedAt: quote.CreatedAt,
        ValidUntil: quote.ValidUntil,
        Notes: quote.Notes,
        ClientName: quote.Client.Name,
        ClientEmail: quote.Client.Email,
        ClientPhone: quote.Client.Phone,
        ClientNif: quote.Client.Nif,
        IssuerName: issuer.FullName,
        IssuerCompany: issuer.CompanyName,
        IssuerEmail: issuer.Email,
        IssuerPhone: issuer.Phone,
        IssuerNif: issuer.Nif,
        IssuerLogoBytes: logoBytes,
        IssuerBrandColorHex: issuer.BrandColor,
        Lines: quote.Lines.ToLineDtos(),
        SubTotal: quote.SubTotal,
        VatTotal: quote.VatTotal,
        Discount: quote.Discount,
        Total: quote.Total,
        ClientAddress: FormatAddress(quote.Client.Address),
        SignatureImage: DecodeDataUrl(quote.SignatureUrl),
        SignedByName: quote.AcceptedByName,
        SignedAt: quote.SignedAt);

    /// <param name="invoice">With Lines and Client loaded.</param>
    /// <param name="issuer">The team owner — the company identity on the document.</param>
    public static InvoicePdfData ForInvoice(Invoice invoice, User issuer, byte[]? logoBytes) => new(
        Number: invoice.Number,
        IssuedAt: invoice.IssuedAt,
        DueDate: invoice.DueDate,
        Notes: invoice.Notes,
        ClientName: invoice.Client.Name,
        ClientEmail: invoice.Client.Email,
        ClientPhone: invoice.Client.Phone,
        ClientNif: invoice.Client.Nif,
        IssuerName: issuer.FullName,
        IssuerCompany: issuer.CompanyName,
        IssuerEmail: issuer.Email,
        IssuerPhone: issuer.Phone,
        IssuerNif: issuer.Nif,
        IssuerLogoBytes: logoBytes,
        IssuerBrandColorHex: issuer.BrandColor,
        IssuerIban: issuer.Iban,
        IssuerBankName: issuer.BankName,
        Lines: invoice.Lines.ToLineDtos(),
        SubTotal: invoice.SubTotal,
        VatTotal: invoice.VatTotal,
        Discount: invoice.Discount,
        Total: invoice.Total,
        ClientAddress: FormatAddress(invoice.Client.Address));

    private static readonly Dictionary<InterventionStatus, string> InterventionStatusLabels = new()
    {
        [InterventionStatus.Scheduled] = "Agendada",
        [InterventionStatus.InProgress] = "Em curso",
        [InterventionStatus.Completed] = "Concluída",
    };

    /// <param name="intervention">With Client and Equipment loaded.</param>
    public static InterventionReportPdfData ForInterventionReport(
        Intervention intervention, User issuer, byte[]? logoBytes, string? technicianName, IReadOnlyList<byte[]> photos) => new(
        Reference: intervention.Id.ToString("N")[..8].ToUpperInvariant(),
        Title: intervention.Title,
        Description: intervention.Description,
        TechnicianNotes: intervention.TechnicianNotes,
        Status: InterventionStatusLabels[intervention.Status],
        ScheduledAt: intervention.ScheduledAt,
        CompletedAt: intervention.CompletedAt,
        TechnicianName: technicianName,
        ClientName: intervention.Client.Name,
        ClientNif: intervention.Client.Nif,
        ClientPhone: intervention.Client.Phone,
        ClientAddress: FormatAddress(intervention.Client.Address),
        IssuerName: issuer.FullName,
        IssuerCompany: issuer.CompanyName,
        IssuerEmail: issuer.Email,
        IssuerPhone: issuer.Phone,
        IssuerNif: issuer.Nif,
        IssuerLogoBytes: logoBytes,
        IssuerBrandColorHex: issuer.BrandColor,
        Equipment: intervention.Equipment
            .Select(e => string.Join(" ", new[] { e.Type, e.Brand, e.Model, e.SerialNumber is null ? null : $"(S/N {e.SerialNumber})" }
                .Where(s => !string.IsNullOrWhiteSpace(s))))
            .ToList(),
        Materials: intervention.Materials.Select(m => (m.Name, m.Quantity)).ToList(),
        LaborHours: intervention.LaborHours,
        Photos: photos,
        SignatureImage: DecodeDataUrl(intervention.ClientSignatureUrl),
        SignedByName: intervention.SignedByName,
        SignedAt: intervention.SignedAt);

    /// <summary>Bytes of a "data:image/...;base64,..." URI (already validated on save); null if absent/invalid.</summary>
    public static byte[]? DecodeDataUrl(string? dataUrl)
    {
        var comma = dataUrl?.IndexOf(',') ?? -1;
        if (comma < 0) return null;
        try { return Convert.FromBase64String(dataUrl![(comma + 1)..]); }
        catch (FormatException) { return null; }
    }

    /// <summary>"Rua das Flores 12, 4000-123 Porto" — null when there's nothing to show.</summary>
    public static string? FormatAddress(Address? address)
    {
        if (address is null) return null;
        var cityLine = string.Join(" ", new[] { address.PostalCode, address.City }.Where(s => !string.IsNullOrWhiteSpace(s)));
        var parts = new[] { address.Street, cityLine }.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        return parts.Count == 0 ? null : string.Join(", ", parts);
    }
}
