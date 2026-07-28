using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.DTOs;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Quotes.DTOs;

namespace TecnicoApp.Application.Features.Quotes.Queries.GetQuotes;

public class GetQuotesQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetQuotesQuery, Result<PaginatedResult<QuoteListItemDto>>>
{
    public async Task<Result<PaginatedResult<QuoteListItemDto>>> Handle(
        GetQuotesQuery request, CancellationToken cancellationToken)
    {
        // Resolve ownerId: team members share their owner's quotes
        var ownerId = await db.Users.AsNoTracking()
            .Where(u => u.Id == currentUser.UserId)
            .Select(u => u.OwnerId ?? u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var query = db.Quotes
            .AsNoTracking()
            .Where(q => q.UserId == ownerId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(q =>
                q.Number.ToLower().Contains(search) ||
                q.Client.Name.ToLower().Contains(search));
        }

        if (request.Status.HasValue)
            query = query.Where(q => q.Status == request.Status.Value);

        if (request.ClientId.HasValue)
            query = query.Where(q => q.ClientId == request.ClientId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(q => q.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(q => new QuoteListItemDto(
                q.Id,
                q.Number,
                q.Status,
                q.Client.Name,
                q.Lines.Sum(l => l.Quantity * l.UnitPrice * (1 + l.VatRate / 100)) - (q.Discount ?? 0),
                q.ValidUntil,
                q.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return Result.Success(new PaginatedResult<QuoteListItemDto>(
            items, totalCount, request.Page, request.PageSize));
    }
}
