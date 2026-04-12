using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Quotes.DTOs;

namespace TecnicoApp.Application.Features.Quotes.Queries.GetQuoteById;

public class GetQuoteByIdQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetQuoteByIdQuery, Result<QuoteDto>>
{
    public async Task<Result<QuoteDto>> Handle(
        GetQuoteByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var quote = await db.Quotes
            .AsNoTracking()
            .Include(q => q.Lines)
            .Include(q => q.Client)
            .FirstOrDefaultAsync(q => q.Id == request.Id, cancellationToken);

        if (quote is null)
            return Result.NotFound();

        if (quote.UserId != userId)
            return Result.Forbidden();

        var dto = new QuoteDto(
            quote.Id,
            quote.Number,
            quote.Status,
            quote.Discount,
            quote.Notes,
            quote.ValidUntil,
            quote.SignedAt,
            quote.PdfUrl,
            quote.ClientId,
            quote.Client.Name,
            quote.SubTotal,
            quote.VatTotal,
            quote.Total,
            quote.Lines
                .OrderBy(l => l.CreatedAt)
                .Select(l => new QuoteLineDto(
                    l.Id,
                    l.Description,
                    l.Quantity,
                    l.UnitPrice,
                    l.VatRate,
                    l.Quantity * l.UnitPrice * (1 + l.VatRate / 100)))
                .ToList(),
            quote.CreatedAt
        );

        return Result.Success(dto);
    }
}
