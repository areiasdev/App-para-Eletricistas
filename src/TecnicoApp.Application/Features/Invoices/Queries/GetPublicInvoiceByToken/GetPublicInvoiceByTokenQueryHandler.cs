using System.Security.Cryptography;
using System.Text;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Invoices.DTOs;

namespace TecnicoApp.Application.Features.Invoices.Queries.GetPublicInvoiceByToken;

public class GetPublicInvoiceByTokenQueryHandler(IAppDbContext db)
    : IRequestHandler<GetPublicInvoiceByTokenQuery, Result<PublicInvoiceDto>>
{
    public async Task<Result<PublicInvoiceDto>> Handle(
        GetPublicInvoiceByTokenQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return Result.NotFound();

        // Same hashing technique as ClientPortalController.Login: hash the incoming raw token
        // and look up the invoice by the stored hash.
        var tokenHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));

        var invoice = await db.Invoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .Include(i => i.Client)
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.PayTokenHash == tokenHash, cancellationToken);

        if (invoice is null || invoice.PayTokenExpiresAt is null || invoice.PayTokenExpiresAt < DateTime.UtcNow)
            return Result.NotFound();

        var dto = new PublicInvoiceDto(
            invoice.Id,
            invoice.Number,
            invoice.Status,
            invoice.Client.Name,
            invoice.User.CompanyName ?? invoice.User.FullName,
            invoice.Total,
            invoice.DueDate,
            invoice.IssuedAt,
            invoice.Lines
                .OrderBy(l => l.CreatedAt)
                .Select(l => new InvoiceLineDto(
                    l.Id, l.Description, l.Quantity, l.UnitPrice, l.VatRate,
                    Math.Round(l.Quantity * l.UnitPrice * (1 + l.VatRate / 100), 2, MidpointRounding.AwayFromZero)))
                .ToList()
        );

        return Result.Success(dto);
    }
}
