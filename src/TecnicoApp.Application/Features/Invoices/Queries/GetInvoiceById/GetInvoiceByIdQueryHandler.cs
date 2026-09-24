using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Invoices.DTOs;
using TecnicoApp.Application.Common.Extensions;

namespace TecnicoApp.Application.Features.Invoices.Queries.GetInvoiceById;

public class GetInvoiceByIdQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetInvoiceByIdQuery, Result<InvoiceDto>>
{
    public async Task<Result<InvoiceDto>> Handle(
        GetInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        // Resolve ownerId: team members share their owner's invoices
        var ownerId = await db.ResolveOwnerIdAsync(currentUser.UserId, cancellationToken);

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

        var dto = invoice.ToDto();

        return Result.Success(dto);
    }
}
