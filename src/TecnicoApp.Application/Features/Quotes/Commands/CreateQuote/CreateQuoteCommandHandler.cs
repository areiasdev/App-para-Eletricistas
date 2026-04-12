using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Quotes.DTOs;
using TecnicoApp.Domain.Entities;

namespace TecnicoApp.Application.Features.Quotes.Commands.CreateQuote;

public class CreateQuoteCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CreateQuoteCommand, Result<QuoteDto>>
{
    public async Task<Result<QuoteDto>> Handle(
        CreateQuoteCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        // Verify client belongs to current user
        var client = await db.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ClientId, cancellationToken);

        if (client is null)
            return Result.NotFound("Cliente não encontrado.");

        if (client.UserId != userId)
            return Result.Forbidden();

        // Generate quote number: ORC-YYYY-NNNN
        var year = DateTime.UtcNow.Year;
        var count = await db.Quotes
            .CountAsync(q => q.UserId == userId && q.CreatedAt.Year == year, cancellationToken);
        var number = $"ORC-{year}-{(count + 1):D4}";

        var quote = new Quote
        {
            Number = number,
            ClientId = request.ClientId,
            UserId = userId,
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
            client.Name,
            quote.SubTotal,
            quote.VatTotal,
            quote.Total,
            quote.Lines
                .Select(l => new QuoteLineDto(
                    l.Id, l.Description, l.Quantity, l.UnitPrice, l.VatRate,
                    l.Quantity * l.UnitPrice * (1 + l.VatRate / 100)))
                .ToList(),
            quote.CreatedAt
        );

        return Result.Success(dto);
    }
}
