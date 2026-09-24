using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Quotes.DTOs;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.Application.Common.Extensions;

namespace TecnicoApp.Application.Features.Quotes.Commands.UpdateQuote;

public class UpdateQuoteCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpdateQuoteCommand, Result<QuoteDto>>
{
    public async Task<Result<QuoteDto>> Handle(
        UpdateQuoteCommand request, CancellationToken cancellationToken)
    {
        // Resolve ownerId: team members share their owner's clients/quotes
        var ownerId = await db.ResolveOwnerIdAsync(currentUser.UserId, cancellationToken);

        var quote = await db.Quotes
            .Include(q => q.Lines)
            .Include(q => q.Client)
            .FirstOrDefaultAsync(q => q.Id == request.Id, cancellationToken);

        if (quote is null)
            return Result.NotFound();

        if (quote.UserId != ownerId)
            return Result.Forbidden();

        if (quote.Status != QuoteStatus.Draft)
            return Result.Error("Só é possível editar orçamentos em rascunho.");

        // Verify client if changed
        if (quote.ClientId != request.ClientId)
        {
            var client = await db.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.ClientId, cancellationToken);

            if (client is null)
                return Result.NotFound("Cliente não encontrado.");

            if (client.UserId != ownerId)
                return Result.Forbidden();

            quote.ClientId = request.ClientId;
            quote.Client = client;
        }

        quote.Discount = request.Discount;
        quote.Notes = request.Notes;
        quote.ValidUntil = request.ValidUntil;
        quote.ModifiedBy = currentUser.Email;

        // Replace all lines. Old lines are removed and new lines are added explicitly via the
        // DbSet rather than through quote.Lines.Clear()/Add() alone: when a new child is
        // introduced to an already-tracked parent purely through collection-navigation fixup,
        // EF Core's change detection sees QuoteLine.Id already has a non-default value (set
        // client-side by BaseEntity's Guid.NewGuid() initializer) and concludes the row might
        // already exist, marking it Modified instead of Added — which issues an UPDATE for a
        // row that was never inserted and throws DbUpdateConcurrencyException at SaveChanges.
        db.QuoteLines.RemoveRange(quote.Lines);
        quote.Lines.Clear();
        foreach (var l in request.Lines)
        {
            // db.QuoteLines.Add (not quote.Lines.Add) marks the row Added directly; EF's
            // relationship fixup then populates quote.Lines from the FK match automatically.
            db.QuoteLines.Add(new QuoteLine
            {
                Description = l.Description,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                VatRate = l.VatRate,
                QuoteId = quote.Id,
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        var dto = quote.ToDto();

        return Result.Success(dto);
    }
}
