using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Quotes.Commands.SignQuote;

public class SignQuoteCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<SignQuoteCommand, Result>
{
    public async Task<Result> Handle(SignQuoteCommand request, CancellationToken cancellationToken)
    {
        // Resolve ownerId: team members share their owner's quotes
        var ownerId = await db.Users.AsNoTracking()
            .Where(u => u.Id == currentUser.UserId)
            .Select(u => u.OwnerId ?? u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var ownerExists = await db.Users.AsNoTracking()
            .AnyAsync(u => u.Id == ownerId, cancellationToken);

        if (!ownerExists) return Result.Unauthorized();

        var quote = await db.Quotes
            .FirstOrDefaultAsync(q => q.Id == request.QuoteId, cancellationToken);

        if (quote is null)
            return Result.NotFound();

        if (quote.UserId != ownerId)
            return Result.Forbidden();

        if (quote.Status is not (QuoteStatus.Sent or QuoteStatus.Accepted))
            return Result.Invalid(new ValidationError(
                "Status",
                "Só é possível assinar orçamentos no estado Enviado ou Aceite."));

        // Only allow safe raster formats (no SVG — SVG can embed script)
        if (string.IsNullOrWhiteSpace(request.SignatureDataUrl) ||
            !System.Text.RegularExpressions.Regex.IsMatch(
                request.SignatureDataUrl,
                @"^data:image/(png|jpeg|gif|webp);base64,[A-Za-z0-9+/]+=*$",
                System.Text.RegularExpressions.RegexOptions.None,
                TimeSpan.FromMilliseconds(100)))
            return Result.Invalid(new ValidationError(
                "SignatureDataUrl",
                "A assinatura deve ser uma imagem PNG, JPEG ou GIF em formato data URI."));

        quote.SignatureUrl = request.SignatureDataUrl;
        quote.SignedAt = DateTime.UtcNow;

        // Automatically move to Accepted if it was only Sent
        if (quote.Status == QuoteStatus.Sent)
            quote.Status = QuoteStatus.Accepted;

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
