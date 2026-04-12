using Ardalis.Result;
using MediatR;
using TecnicoApp.Application.Features.Quotes.Commands.CreateQuote;
using TecnicoApp.Application.Features.Quotes.DTOs;

namespace TecnicoApp.Application.Features.Quotes.Commands.UpdateQuote;

public record UpdateQuoteCommand(
    Guid Id,
    Guid ClientId,
    decimal? Discount,
    string? Notes,
    DateTime? ValidUntil,
    IReadOnlyList<CreateQuoteLineRequest> Lines
) : IRequest<Result<QuoteDto>>;
