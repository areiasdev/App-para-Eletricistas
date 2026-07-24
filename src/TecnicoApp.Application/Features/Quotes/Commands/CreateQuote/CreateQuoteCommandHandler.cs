using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Quotes.DTOs;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Quotes.Commands.CreateQuote;

public class CreateQuoteCommandHandler(IAppDbContext db, ICurrentUserService currentUser, ILogger<CreateQuoteCommandHandler> logger)
    : IRequestHandler<CreateQuoteCommand, Result<QuoteDto>>
{
    public async Task<Result<QuoteDto>> Handle(
        CreateQuoteCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        // Resolve ownerId: team members share their owner's clients/quotes
        var ownerId = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.OwnerId ?? u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var ownerExists = await db.Users.AsNoTracking()
            .AnyAsync(u => u.Id == ownerId, cancellationToken);

        if (!ownerExists) return Result.Unauthorized();

        // Verify client belongs to the owner's team
        var client = await db.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ClientId, cancellationToken);

        if (client is null)
            return Result.NotFound("Cliente não encontrado.");

        if (client.UserId != ownerId)
            return Result.Forbidden();

        // Generate quote number: ORC-YYYY-NNNN
        var year = DateTime.UtcNow.Year;
        var count = await db.Quotes
            .CountAsync(q => q.UserId == ownerId && q.CreatedAt.Year == year, cancellationToken);
        var number = $"ORC-{year}-{(count + 1):D4}";

        var quote = new Quote
        {
            Number = number,
            ClientId = request.ClientId,
            UserId = ownerId,
            Discount = request.Discount,
            Notes = request.Notes,
            ValidUntil = request.ValidUntil,
            Lines = request.Lines.Select(l => new QuoteLine
            {
                Description = l.Description,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                VatRate = l.VatRate,
            }).ToList(),
        };

        db.Quotes.Add(quote);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("quotes_number") == true)
        {
            // Unique constraint violation on Number — concurrent request generated same number
            logger.LogWarning("Quote number conflict for {Number}, retrying.", number);
            db.Quotes.Remove(quote);
            var retryCount = await db.Quotes
                .CountAsync(q => q.UserId == ownerId && q.CreatedAt.Year == year, cancellationToken);
            quote.Number = $"ORC-{year}-{(retryCount + 1):D4}";
            db.Quotes.Add(quote);
            await db.SaveChangesAsync(cancellationToken);
        }

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
            client.Name,
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
