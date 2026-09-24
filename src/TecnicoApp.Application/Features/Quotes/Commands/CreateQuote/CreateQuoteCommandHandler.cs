using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Quotes.DTOs;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.Application.Common.Extensions;

namespace TecnicoApp.Application.Features.Quotes.Commands.CreateQuote;

public class CreateQuoteCommandHandler(IAppDbContext db, ICurrentUserService currentUser, ILogger<CreateQuoteCommandHandler> logger)
    : IRequestHandler<CreateQuoteCommand, Result<QuoteDto>>
{
    public async Task<Result<QuoteDto>> Handle(
        CreateQuoteCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        // Resolve ownerId: team members share their owner's clients/quotes
        var ownerId = await db.ResolveOwnerIdAsync(userId, cancellationToken);

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

        var quote = new Quote
        {
            Number = string.Empty, // assigned by AddQuoteWithNextNumberAsync (ORC-YYYY-NNNN)
            ClientId = request.ClientId,
            UserId = ownerId,
            Discount = request.Discount,
            Notes = request.Notes,
            ValidUntil = request.ValidUntil,
            Lines = request.Lines.Select((l, index) => l.ToQuoteLine(index)).ToList(),
        };

        await db.AddQuoteWithNextNumberAsync(quote, ownerId, logger, cancellationToken);

        var dto = quote.ToDto(client.Name);

        return Result.Success(dto);
    }
}
