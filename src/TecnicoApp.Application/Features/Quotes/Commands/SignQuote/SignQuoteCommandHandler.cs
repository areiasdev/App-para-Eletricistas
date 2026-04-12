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
        var userId = currentUser.UserId;

        var quote = await db.Quotes
            .FirstOrDefaultAsync(q => q.Id == request.QuoteId, cancellationToken);

        if (quote is null)
            return Result.NotFound();

        if (quote.UserId != userId)
            return Result.Forbidden();

        if (quote.Status is not (QuoteStatus.Sent or QuoteStatus.Accepted))
            return Result.Invalid(new ValidationError(
                "Status",
                "Só é possível assinar orçamentos no estado Enviado ou Aceite."));

        if (string.IsNullOrWhiteSpace(request.SignatureDataUrl) ||
            !request.SignatureDataUrl.StartsWith("data:image/"))
            return Result.Invalid(new ValidationError(
                "SignatureDataUrl",
                "A assinatura deve ser uma imagem em formato data URI."));

        quote.SignatureUrl = request.SignatureDataUrl;
        quote.SignedAt = DateTime.UtcNow;

        // Automatically move to Accepted if it was only Sent
        if (quote.Status == QuoteStatus.Sent)
            quote.Status = QuoteStatus.Accepted;

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
