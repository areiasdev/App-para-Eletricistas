using Ardalis.Result;
using MediatR;
using TecnicoApp.Application.Features.Quotes.DTOs;
using TecnicoApp.Domain.Common;

namespace TecnicoApp.Application.Features.Quotes.Commands.CreateQuote;

public record CreateQuoteLineRequest(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal VatRate = DocumentMath.StandardVatRate
);

public record CreateQuoteCommand(
    Guid ClientId,
    decimal? Discount,
    string? Notes,
    DateTime? ValidUntil,
    IReadOnlyList<CreateQuoteLineRequest> Lines
) : IRequest<Result<QuoteDto>>;
