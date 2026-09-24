using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Documents;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Quotes.DTOs;
using TecnicoApp.Application.Common.Extensions;

namespace TecnicoApp.Application.Features.Quotes.Queries.GenerateQuotePdf;

public class GenerateQuotePdfQueryHandler(
    IAppDbContext db,
    ICurrentUserService currentUser,
    IPdfService pdfService,
    IFileStorageService fileStorage)
    : IRequestHandler<GenerateQuotePdfQuery, Result<QuotePdfResult>>
{
    public async Task<Result<QuotePdfResult>> Handle(
        GenerateQuotePdfQuery request, CancellationToken cancellationToken)
    {
        // Resolve ownerId: team members share their owner's quotes
        var ownerId = await db.ResolveOwnerIdAsync(currentUser.UserId, cancellationToken);

        var quote = await db.Quotes
            .AsNoTracking()
            .Include(q => q.Lines)
            .Include(q => q.Client)
            .Include(q => q.User) // quote.User is always the team owner — see CreateQuoteCommandHandler
            .FirstOrDefaultAsync(q => q.Id == request.QuoteId, cancellationToken);

        if (quote is null)
            return Result.NotFound();

        if (quote.UserId != ownerId)
            return Result.Forbidden();

        var logoBytes = await fileStorage.ReadUploadBytesAsync(quote.User.LogoUrl, cancellationToken);

        var pdfData = PdfDataFactory.ForQuote(quote, quote.User, logoBytes);

        var bytes = pdfService.GenerateQuotePdf(pdfData);
        return Result.Success(new QuotePdfResult(bytes, quote.Number));
    }
}
