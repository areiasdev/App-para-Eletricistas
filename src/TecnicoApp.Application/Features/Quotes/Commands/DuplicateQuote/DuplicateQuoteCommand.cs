using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecnicoApp.Application.Common.Extensions;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Quotes.DTOs;
using TecnicoApp.Domain.Entities;

namespace TecnicoApp.Application.Features.Quotes.Commands.DuplicateQuote;

/// <summary>
/// Copies a quote (any status) into a new Draft with a fresh number — for recurring jobs
/// ("mesmo quadro que o do vizinho") or to revise a quote that was already sent/rejected.
/// </summary>
public record DuplicateQuoteCommand(Guid Id) : IRequest<Result<QuoteDto>>;

public class DuplicateQuoteCommandHandler(
    IAppDbContext db, ICurrentUserService currentUser, ILogger<DuplicateQuoteCommandHandler> logger)
    : IRequestHandler<DuplicateQuoteCommand, Result<QuoteDto>>
{
    public async Task<Result<QuoteDto>> Handle(DuplicateQuoteCommand request, CancellationToken cancellationToken)
    {
        var ownerId = await db.ResolveOwnerIdAsync(currentUser.UserId, cancellationToken);

        var source = await db.Quotes
            .AsNoTracking()
            .Include(q => q.Lines)
            .Include(q => q.Client)
            .FirstOrDefaultAsync(q => q.Id == request.Id, cancellationToken);

        if (source is null) return Result.NotFound();
        if (source.UserId != ownerId) return Result.Forbidden();

        var validity = source.ValidUntil - source.CreatedAt;

        var copy = new Quote
        {
            Number = string.Empty, // assigned by AddQuoteWithNextNumberAsync
            ClientId = source.ClientId,
            UserId = ownerId,
            Discount = source.Discount,
            Notes = source.Notes,
            // Keep the same validity period, counted from today.
            ValidUntil = validity is { } period && period > TimeSpan.Zero ? DateTime.UtcNow.Date.Add(period) : null,
            ModifiedBy = currentUser.Email,
            Lines = source.Lines
                .OrderBy(l => l.Position).ThenBy(l => l.CreatedAt)
                .Select((l, index) => new QuoteLine
                {
                    Description = l.Description,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice,
                    VatRate = l.VatRate,
                    Unit = l.Unit,
                    Position = index,
                })
                .ToList(),
        };

        await db.AddQuoteWithNextNumberAsync(copy, ownerId, logger, cancellationToken);

        return Result.Success(copy.ToDto(source.Client.Name));
    }
}
