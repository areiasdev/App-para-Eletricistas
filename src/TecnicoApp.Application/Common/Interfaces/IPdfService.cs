using TecnicoApp.Application.Features.Invoices.DTOs;
using TecnicoApp.Application.Features.Quotes.DTOs;

namespace TecnicoApp.Application.Common.Interfaces;

public record QuotePdfData(
    string Number,
    DateTime CreatedAt,
    DateTime? ValidUntil,
    string? Notes,
    // Client
    string ClientName,
    string? ClientEmail,
    string? ClientPhone,
    string? ClientNif,
    // Issuer (technician / company)
    string IssuerName,
    string? IssuerCompany,
    string? IssuerEmail,
    string? IssuerPhone,
    string? IssuerNif,
    byte[]? IssuerLogoBytes,
    string? IssuerBrandColorHex,
    // Lines & totals
    IReadOnlyList<QuoteLineDto> Lines,
    decimal SubTotal,
    decimal VatTotal,
    decimal? Discount,
    decimal Total,
    string? ClientAddress = null,
    byte[]? SignatureImage = null,
    string? SignedByName = null,
    DateTime? SignedAt = null
);

public record InvoicePdfData(
    string Number,
    DateTime IssuedAt,
    DateTime DueDate,
    string? Notes,
    // Client
    string ClientName,
    string? ClientEmail,
    string? ClientPhone,
    string? ClientNif,
    // Issuer (technician / company)
    string IssuerName,
    string? IssuerCompany,
    string? IssuerEmail,
    string? IssuerPhone,
    string? IssuerNif,
    byte[]? IssuerLogoBytes,
    string? IssuerBrandColorHex,
    string? IssuerIban,
    string? IssuerBankName,
    // Lines & totals
    IReadOnlyList<InvoiceLineDto> Lines,
    decimal SubTotal,
    decimal VatTotal,
    decimal? Discount,
    decimal Total,
    string? ClientAddress = null
);

public record InterventionReportPdfData(
    string Reference,
    string Title,
    string? Description,
    string? TechnicianNotes,
    string Status,
    DateTime? ScheduledAt,
    DateTime? CompletedAt,
    string? TechnicianName,
    // Client
    string ClientName,
    string? ClientNif,
    string? ClientPhone,
    string? ClientAddress,
    // Issuer
    string IssuerName,
    string? IssuerCompany,
    string? IssuerEmail,
    string? IssuerPhone,
    string? IssuerNif,
    byte[]? IssuerLogoBytes,
    string? IssuerBrandColorHex,
    // Work
    IReadOnlyList<string> Equipment,
    IReadOnlyList<(string Name, decimal Quantity)> Materials,
    decimal? LaborHours,
    IReadOnlyList<byte[]> Photos,
    // Sign-off
    byte[]? SignatureImage,
    string? SignedByName,
    DateTime? SignedAt
);

public interface IPdfService
{
    byte[] GenerateQuotePdf(QuotePdfData data);
    byte[] GenerateInvoicePdf(InvoicePdfData data);
    byte[] GenerateInterventionReportPdf(InterventionReportPdfData data);
}
