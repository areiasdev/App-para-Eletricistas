using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.DTOs;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Invoices.DTOs;

namespace TecnicoApp.Application.Features.Invoices.Queries.GetInvoices;

public class GetInvoicesQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetInvoicesQuery, Result<PaginatedResult<InvoiceListItemDto>>>
{
    public async Task<Result<PaginatedResult<InvoiceListItemDto>>> Handle(
        GetInvoicesQuery request, CancellationToken cancellationToken)
    {
        // Resolve ownerId: team members share their owner's invoices
        var ownerId = await db.Users.AsNoTracking()
            .Where(u => u.Id == currentUser.UserId)
            .Select(u => u.OwnerId ?? u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var query = db.Invoices
            .AsNoTracking()
            .Where(i => i.UserId == ownerId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(i =>
                i.Number.ToLower().Contains(search) ||
                i.Client.Name.ToLower().Contains(search));
        }

        if (request.Status.HasValue)
            query = query.Where(i => i.Status == request.Status.Value);

        if (request.ClientId.HasValue)
            query = query.Where(i => i.ClientId == request.ClientId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(i => new InvoiceListItemDto(
                i.Id,
                i.Number,
                i.Status,
                i.Client.Name,
                i.Lines.Sum(l => l.Quantity * l.UnitPrice * (1 + l.VatRate / 100)) - (i.Discount ?? 0),
                i.DueDate,
                i.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return Result.Success(new PaginatedResult<InvoiceListItemDto>(
            items, totalCount, request.Page, request.PageSize));
    }
}
