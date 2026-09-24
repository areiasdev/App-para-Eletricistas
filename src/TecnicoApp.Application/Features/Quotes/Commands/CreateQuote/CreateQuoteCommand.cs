using Ardalis.Result;
using MediatR;
using TecnicoApp.Application.Features.Quotes.DTOs;
using TecnicoApp.Domain.Common;
using TecnicoApp.Domain.Entities;

namespace TecnicoApp.Application.Features.Quotes.Commands.CreateQuote;

public record CreateQuoteLineRequest(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal VatRate = DocumentMath.StandardVatRate,
    string? Unit = null
);

public record CreateQuoteCommand(
    Guid ClientId,
    decimal? Discount,
    string? Notes,
    DateTime? ValidUntil,
    IReadOnlyList<CreateQuoteLineRequest> Lines
) : IRequest<Result<QuoteDto>>;

public static class CreateQuoteLineRequestExtensions
{
    public static QuoteLine ToQuoteLine(this CreateQuoteLineRequest request, int position) => new()
    {
        Description = request.Description,
        Quantity = request.Quantity,
        UnitPrice = request.UnitPrice,
        VatRate = request.VatRate,
        Unit = string.IsNullOrWhiteSpace(request.Unit) ? DocumentMath.DefaultUnit : request.Unit.Trim(),
        Position = position,
    };
}
