using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.DTOs;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Interventions.DTOs;

namespace TecnicoApp.Application.Features.Interventions.Queries.GetInterventions;

public class GetInterventionsQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetInterventionsQuery, Result<PaginatedResult<InterventionListItemDto>>>
{
    public async Task<Result<PaginatedResult<InterventionListItemDto>>> Handle(
        GetInterventionsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var query = db.Interventions
            .AsNoTracking()
            .Where(i => i.UserId == userId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.ToLower();
            query = query.Where(i =>
                i.Title.ToLower().Contains(s) ||
                i.Client.Name.ToLower().Contains(s));
        }

        if (request.Status.HasValue)
            query = query.Where(i => i.Status == request.Status.Value);

        if (request.ClientId.HasValue)
            query = query.Where(i => i.ClientId == request.ClientId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(i => i.ScheduledAt ?? i.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(i => new InterventionListItemDto(
                i.Id,
                i.Title,
                i.Status,
                i.ScheduledAt,
                i.CompletedAt,
                i.ClientId,
                i.Client.Name,
                i.Equipment.Count,
                i.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return Result.Success(new PaginatedResult<InterventionListItemDto>(
            items, totalCount, request.Page, request.PageSize));
    }
}
