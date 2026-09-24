using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Quotes.DTOs;
using TecnicoApp.Application.Common.Extensions;

namespace TecnicoApp.Application.Features.Quotes.Queries.GetQuoteById;

public class GetQuoteByIdQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetQuoteByIdQuery, Result<QuoteDto>>
{
    public async Task<Result<QuoteDto>> Handle(
        GetQuoteByIdQuery request, CancellationToken cancellationToken)
    {
        // Resolve ownerId: team members share their owner's quotes
        var ownerId = await db.ResolveOwnerIdAsync(currentUser.UserId, cancellationToken);

        var quote = await db.Quotes
            .AsNoTracking()
            .Include(q => q.Lines)
            .Include(q => q.Client)
            .FirstOrDefaultAsync(q => q.Id == request.Id, cancellationToken);

        if (quote is null)
            return Result.NotFound();

        if (quote.UserId != ownerId)
            return Result.Forbidden();

        var dto = quote.ToDto();

        return Result.Success(dto);
    }
}
