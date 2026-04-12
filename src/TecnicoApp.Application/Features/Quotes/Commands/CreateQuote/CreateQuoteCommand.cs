using Ardalis.Result;
using MediatR;
using TecnicoApp.Application.Features.Quotes.DTOs;

namespace TecnicoApp.Application.Features.Quotes.Commands.CreateQuote;

public record CreateQuoteLineRequest(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal VatRate = 23m
);

public record CreateQuoteCommand(
    Guid ClientId,
    decimal? Discount,
    string? Notes,
    DateTime? ValidUntil,
    IReadOnlyList<CreateQuoteLineRequest> Lines
) : IRequest<Result<QuoteDto>>;
