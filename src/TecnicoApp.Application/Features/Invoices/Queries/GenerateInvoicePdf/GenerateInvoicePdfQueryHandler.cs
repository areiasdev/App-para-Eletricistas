using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Documents;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Invoices.DTOs;
using TecnicoApp.Application.Common.Extensions;

namespace TecnicoApp.Application.Features.Invoices.Queries.GenerateInvoicePdf;

public class GenerateInvoicePdfQueryHandler(
    IAppDbContext db,
    ICurrentUserService currentUser,
    IPdfService pdfService,
    IFileStorageService fileStorage)
    : IRequestHandler<GenerateInvoicePdfQuery, Result<InvoicePdfResult>>
{
    public async Task<Result<InvoicePdfResult>> Handle(
        GenerateInvoicePdfQuery request, CancellationToken cancellationToken)
    {
        // Resolve ownerId: team members share their owner's invoices
        var ownerId = await db.ResolveOwnerIdAsync(currentUser.UserId, cancellationToken);

        var invoice = await db.Invoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .Include(i => i.Client)
            .Include(i => i.User) // invoice.User is always the team owner — see CreateInvoiceFromQuoteCommandHandler
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

        if (invoice is null)
            return Result.NotFound();

        if (invoice.UserId != ownerId)
            return Result.Forbidden();

        var logoBytes = await fileStorage.ReadUploadBytesAsync(invoice.User.LogoUrl, cancellationToken);

        var pdfData = PdfDataFactory.ForInvoice(invoice, invoice.User, logoBytes);

        var bytes = pdfService.GenerateInvoicePdf(pdfData);
        return Result.Success(new InvoicePdfResult(bytes, invoice.Number));
    }
}
