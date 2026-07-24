using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Quotes.Commands.DeleteQuote;

public class DeleteQuoteCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<DeleteQuoteCommand, Result>
{
    public async Task<Result> Handle(
        DeleteQuoteCommand request, CancellationToken cancellationToken)
    {
        // Resolve ownerId: team members share their owner's quotes
        var ownerId = await db.Users.AsNoTracking()
            .Where(u => u.Id == currentUser.UserId)
            .Select(u => u.OwnerId ?? u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var quote = await db.Quotes
            .FirstOrDefaultAsync(q => q.Id == request.Id, cancellationToken);

        if (quote is null)
            return Result.NotFound();

        if (quote.UserId != ownerId)
            return Result.Forbidden();

        if (quote.Status != QuoteStatus.Draft)
            return Result.Error("Só é possível apagar orçamentos em rascunho.");

        quote.IsDeleted = true;
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
