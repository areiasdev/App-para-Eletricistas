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
        // Resolve ownerId: team members share their owner's quotes
        var ownerId = await db.Users.AsNoTracking()
            .Where(u => u.Id == currentUser.UserId)
            .Select(u => u.OwnerId ?? u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var quote = await db.Quotes
            .AsNoTracking()
            .Include(q => q.Lines)
            .Include(q => q.Client)
            .FirstOrDefaultAsync(q => q.Id == request.Id, cancellationToken);

        if (quote is null)
            return Result.NotFound();

        if (quote.UserId != ownerId)
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
                    Math.Round(l.Quantity * l.UnitPrice * (1 + l.VatRate / 100), 2, MidpointRounding.AwayFromZero)))
                .ToList(),
            quote.CreatedAt,
            quote.EmailSentAt
        );

        return Result.Success(dto);
    }
}
