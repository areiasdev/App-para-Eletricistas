using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.DTOs;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Clients.DTOs;

namespace TecnicoApp.Application.Features.Clients.Queries.GetClients;

public sealed class GetClientsQueryHandler(
    IAppDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetClientsQuery, Result<PaginatedResult<ClientListItemDto>>>
{
    public async Task<Result<PaginatedResult<ClientListItemDto>>> Handle(
        GetClientsQuery query,
        CancellationToken cancellationToken)
    {
        var q = db.Clients
            .AsNoTracking()
            .Where(c => c.UserId == currentUser.UserId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.ToLowerInvariant();
            q = q.Where(c =>
                c.Name.ToLower().Contains(search) ||
                (c.Email != null && c.Email.ToLower().Contains(search)) ||
                (c.Phone != null && c.Phone.Contains(search)));
        }

        var totalCount = await q.CountAsync(cancellationToken);

        var items = await q
            .OrderBy(c => c.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new ClientListItemDto(
                c.Id,
                c.Name,
                c.Email,
                c.Phone,
                c.Quotes.Count(qu => !qu.IsDeleted),
                c.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(new PaginatedResult<ClientListItemDto>(
            items, totalCount, query.Page, query.PageSize));
    }
}
