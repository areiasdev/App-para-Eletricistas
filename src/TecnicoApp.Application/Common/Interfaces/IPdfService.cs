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
    decimal Total
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
    decimal Total
);

public interface IPdfService
{
    byte[] GenerateQuotePdf(QuotePdfData data);
    byte[] GenerateInvoicePdf(InvoicePdfData data);
}
