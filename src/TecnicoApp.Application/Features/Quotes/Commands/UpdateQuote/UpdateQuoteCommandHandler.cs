using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Quotes.DTOs;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Quotes.Commands.UpdateQuote;

public class UpdateQuoteCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpdateQuoteCommand, Result<QuoteDto>>
{
    public async Task<Result<QuoteDto>> Handle(
        UpdateQuoteCommand request, CancellationToken cancellationToken)
    {
        // Resolve ownerId: team members share their owner's clients/quotes
        var ownerId = await db.Users.AsNoTracking()
            .Where(u => u.Id == currentUser.UserId)
            .Select(u => u.OwnerId ?? u.Id)
            .FirstOrDefaultAsync(cancellationToken);

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

        // Replace all lines
        quote.Lines.Clear();
        foreach (var l in request.Lines)
        {
            quote.Lines.Add(new QuoteLine
            {
                Description = l.Description,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                VatRate = l.VatRate,
                QuoteId = quote.Id,
            });
        }

        await db.SaveChangesAsync(cancellationToken);

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
                .Select(l => new QuoteLineDto(
                    l.Id, l.Description, l.Quantity, l.UnitPrice, l.VatRate,
                    Math.Round(l.Quantity * l.UnitPrice * (1 + l.VatRate / 100), 2, MidpointRounding.AwayFromZero)))
                .ToList(),
            quote.CreatedAt,
            quote.EmailSentAt
        );

        return Result.Success(dto);
    }
}
