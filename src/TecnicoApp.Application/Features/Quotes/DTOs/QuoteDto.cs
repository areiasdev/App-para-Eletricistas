using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Quotes.DTOs;

public record QuoteLineDto(
    Guid Id,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal VatRate,
    decimal LineTotal
);

public record QuoteDto(
    Guid Id,
    string Number,
    QuoteStatus Status,
    decimal? Discount,
    string? Notes,
    DateTime? ValidUntil,
    DateTime? SignedAt,
    string? PdfUrl,
    Guid ClientId,
    string ClientName,
    decimal SubTotal,
    decimal VatTotal,
    decimal Total,
    IReadOnlyList<QuoteLineDto> Lines,
    DateTime CreatedAt
);

public record QuoteListItemDto(
    Guid Id,
    string Number,
    QuoteStatus Status,
    string ClientName,
    decimal Total,
    DateTime? ValidUntil,
    DateTime CreatedAt
);
