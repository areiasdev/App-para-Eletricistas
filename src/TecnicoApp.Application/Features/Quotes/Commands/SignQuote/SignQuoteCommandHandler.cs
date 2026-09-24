using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Common.Security;
using TecnicoApp.Domain.Enums;
using TecnicoApp.Application.Common.Extensions;

namespace TecnicoApp.Application.Features.Quotes.Commands.SignQuote;

public class SignQuoteCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<SignQuoteCommand, Result>
{
    public async Task<Result> Handle(SignQuoteCommand request, CancellationToken cancellationToken)
    {
        // Resolve ownerId: team members share their owner's quotes
        var ownerId = await db.ResolveOwnerIdAsync(currentUser.UserId, cancellationToken);

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
        if (!PublicTokens.IsValidSignature(request.SignatureDataUrl))
            return Result.Invalid(new ValidationError(
                "SignatureDataUrl",
                "A assinatura deve ser uma imagem PNG, JPEG ou WEBP em formato data URI."));

        quote.SignatureUrl = request.SignatureDataUrl;
        quote.SignedAt = DateTime.UtcNow;

        // Automatically move to Accepted if it was only Sent
        if (quote.Status == QuoteStatus.Sent)
            quote.Status = QuoteStatus.Accepted;

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
