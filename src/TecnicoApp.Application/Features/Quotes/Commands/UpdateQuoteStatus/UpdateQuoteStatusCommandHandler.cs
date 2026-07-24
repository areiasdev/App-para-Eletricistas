using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Quotes.Commands.UpdateQuoteStatus;

public class UpdateQuoteStatusCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpdateQuoteStatusCommand, Result>
{
    public async Task<Result> Handle(
        UpdateQuoteStatusCommand request, CancellationToken cancellationToken)
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

        // Validate status transitions
        var valid = (quote.Status, request.Status) switch
        {
            (QuoteStatus.Draft, QuoteStatus.Sent) => true,
            (QuoteStatus.Sent, QuoteStatus.Accepted) => true,
            (QuoteStatus.Sent, QuoteStatus.Rejected) => true,
            (QuoteStatus.Accepted, QuoteStatus.Invoiced) => true,
            (QuoteStatus.Sent, QuoteStatus.Draft) => true,   // allow recall
            _ => false
        };

        if (!valid)
            return Result.Error($"Transição de estado inválida: {quote.Status} → {request.Status}.");

        quote.Status = request.Status;
        quote.ModifiedBy = currentUser.Email;
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
