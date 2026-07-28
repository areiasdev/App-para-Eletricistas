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
        var caller = await db.Users.AsNoTracking()
            .Where(u => u.Id == currentUser.UserId)
            .Select(u => new { OwnerId = u.OwnerId ?? u.Id, u.Role })
            .FirstOrDefaultAsync(cancellationToken);

        if (caller is null)
            return Result.Unauthorized();

        // Deletes are irreversible from the UI — restrict to Owner/Admin so a technician
        // can't wipe out company records unsupervised.
        if (caller.Role is not (UserRole.Owner or UserRole.Admin))
            return Result.Forbidden("Apenas o proprietário ou administradores podem apagar orçamentos.");

        var ownerId = caller.OwnerId;

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
