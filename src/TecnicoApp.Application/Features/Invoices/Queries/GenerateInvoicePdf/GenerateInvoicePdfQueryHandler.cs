using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
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

        var lines = invoice.Lines.ToLineDtos();

        var logoBytes = await fileStorage.ReadLogoBytesAsync(invoice.User.LogoUrl, cancellationToken);

        var pdfData = new InvoicePdfData(
            Number: invoice.Number,
            IssuedAt: invoice.IssuedAt,
            DueDate: invoice.DueDate,
            Notes: invoice.Notes,
            ClientName: invoice.Client.Name,
            ClientEmail: invoice.Client.Email,
            ClientPhone: invoice.Client.Phone,
            ClientNif: invoice.Client.Nif,
            IssuerName: invoice.User.FullName,
            IssuerCompany: invoice.User.CompanyName,
            IssuerEmail: invoice.User.Email,
            IssuerPhone: invoice.User.Phone,
            IssuerNif: invoice.User.Nif,
            IssuerLogoBytes: logoBytes,
            IssuerBrandColorHex: invoice.User.BrandColor,
            IssuerIban: invoice.User.Iban,
            IssuerBankName: invoice.User.BankName,
            Lines: lines,
            SubTotal: invoice.SubTotal,
            VatTotal: invoice.VatTotal,
            Discount: invoice.Discount,
            Total: invoice.Total
        );

        var bytes = pdfService.GenerateInvoicePdf(pdfData);
        return Result.Success(new InvoicePdfResult(bytes, invoice.Number));
    }
}
