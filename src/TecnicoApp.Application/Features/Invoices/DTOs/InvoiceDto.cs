using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Invoices.DTOs;

public record InvoiceLineDto(
    Guid Id,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal VatRate,
    decimal LineTotal
);

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
