using TecnicoApp.Domain.Common;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Invoices.DTOs;

public record InvoiceLineDto(
    Guid Id,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal VatRate,
    decimal LineTotal,
    string Unit = DocumentMath.DefaultUnit
) : IDocumentLine;

public record InvoiceDto(
    Guid Id,
    string Number,
    InvoiceStatus Status,
    decimal? Discount,
    string? Notes,
    DateTime IssuedAt,
    DateTime DueDate,
    DateTime? PaidAt,
    Guid ClientId,
    string ClientName,
    Guid? QuoteId,
    string? QuoteNumber,
    decimal SubTotal,
    decimal VatTotal,
    decimal Total,
    IReadOnlyList<InvoiceLineDto> Lines,
    DateTime CreatedAt
);

public record InvoiceListItemDto(
    Guid Id,
    string Number,
    InvoiceStatus Status,
    string ClientName,
    decimal Total,
    DateTime DueDate,
    DateTime CreatedAt
);

// Minimal, deliberately non-sensitive summary for the public/unauthenticated "Pagar agora"
// page — reachable only via a magic-link token, not a logged-in session. No client contact
// details, no owner-internal notes; just enough to render a payment confirmation screen.
public record PublicInvoiceDto(
    Guid Id,
    string Number,
    InvoiceStatus Status,
    string ClientName,
    string IssuerCompanyName,
    decimal Total,
    DateTime DueDate,
    DateTime IssuedAt,
    IReadOnlyList<InvoiceLineDto> Lines
);
