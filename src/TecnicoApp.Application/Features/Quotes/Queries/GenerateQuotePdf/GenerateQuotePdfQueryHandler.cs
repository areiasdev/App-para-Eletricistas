using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Quotes.DTOs;

namespace TecnicoApp.Application.Features.Quotes.Queries.GenerateQuotePdf;

public class GenerateQuotePdfQueryHandler(
    IAppDbContext db,
    ICurrentUserService currentUser,
    IPdfService pdfService)
    : IRequestHandler<GenerateQuotePdfQuery, Result<QuotePdfResult>>
{
    public async Task<Result<QuotePdfResult>> Handle(
        GenerateQuotePdfQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var quote = await db.Quotes
            .AsNoTracking()
            .Include(q => q.Lines)
            .Include(q => q.Client)
            .Include(q => q.User)
            .FirstOrDefaultAsync(q => q.Id == request.QuoteId, cancellationToken);

        if (quote is null)
            return Result.NotFound();

        if (quote.UserId != userId)
            return Result.Forbidden();

        var lines = quote.Lines
            .OrderBy(l => l.CreatedAt)
            .Select(l => new QuoteLineDto(
                l.Id,
                l.Description,
                l.Quantity,
                l.UnitPrice,
                l.VatRate,
                l.Quantity * l.UnitPrice * (1 + l.VatRate / 100)))
            .ToList();

        var pdfData = new QuotePdfData(
            Number: quote.Number,
            CreatedAt: quote.CreatedAt,
            ValidUntil: quote.ValidUntil,
            Notes: quote.Notes,
            ClientName: quote.Client.Name,
            ClientEmail: quote.Client.Email,
            ClientPhone: quote.Client.Phone,
            ClientNif: quote.Client.Nif,
            IssuerName: quote.User.FullName,
            IssuerCompany: quote.User.CompanyName,
            IssuerEmail: quote.User.Email,
            IssuerPhone: quote.User.Phone,
            IssuerNif: quote.User.Nif,
            Lines: lines,
            SubTotal: quote.SubTotal,
            VatTotal: quote.VatTotal,
            Discount: quote.Discount,
            Total: quote.Total
        );

        var bytes = pdfService.GenerateQuotePdf(pdfData);
        return Result.Success(new QuotePdfResult(bytes, quote.Number));
    }
}
