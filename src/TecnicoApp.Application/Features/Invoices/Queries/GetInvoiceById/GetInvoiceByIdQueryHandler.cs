using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Invoices.DTOs;

namespace TecnicoApp.Application.Features.Invoices.Queries.GetInvoiceById;

public class GetInvoiceByIdQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetInvoiceByIdQuery, Result<InvoiceDto>>
{
    public async Task<Result<InvoiceDto>> Handle(
        GetInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        // Resolve ownerId: team members share their owner's invoices
        var ownerId = await db.Users.AsNoTracking()
            .Where(u => u.Id == currentUser.UserId)
            .Select(u => u.OwnerId ?? u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var invoice = await db.Invoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .Include(i => i.Client)
            .Include(i => i.Quote)
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        if (invoice is null)
            return Result.NotFound();

        if (invoice.UserId != ownerId)
            return Result.Forbidden();

        var dto = new InvoiceDto(
            invoice.Id,
            invoice.Number,
            invoice.Status,
            invoice.Discount,
            invoice.Notes,
            invoice.IssuedAt,
            invoice.DueDate,
            invoice.PaidAt,
            invoice.ClientId,
            invoice.Client.Name,
            invoice.QuoteId,
            invoice.Quote?.Number,
            invoice.SubTotal,
            invoice.VatTotal,
            invoice.Total,
            invoice.Lines
                .OrderBy(l => l.CreatedAt)
                .Select(l => new InvoiceLineDto(
                    l.Id,
                    l.Description,
                    l.Quantity,
                    l.UnitPrice,
                    l.VatRate,
                    Math.Round(l.Quantity * l.UnitPrice * (1 + l.VatRate / 100), 2, MidpointRounding.AwayFromZero)))
                .ToList(),
            invoice.CreatedAt
        );

        return Result.Success(dto);
    }
}
