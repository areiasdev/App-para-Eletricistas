using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.DTOs;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Equipment.DTOs;

namespace TecnicoApp.Application.Features.Equipment.Queries.GetEquipment;

public class GetEquipmentQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetEquipmentQuery, Result<PaginatedResult<EquipmentListItemDto>>>
{
    public async Task<Result<PaginatedResult<EquipmentListItemDto>>> Handle(
        GetEquipmentQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var query = db.Equipment
            .AsNoTracking()
            .Where(e => e.Client.UserId == userId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.ToLower();
            query = query.Where(e =>
                e.Type.ToLower().Contains(s) ||
                (e.Brand != null && e.Brand.ToLower().Contains(s)) ||
                (e.Model != null && e.Model.ToLower().Contains(s)) ||
                (e.SerialNumber != null && e.SerialNumber.ToLower().Contains(s)));
        }

        if (request.ClientId.HasValue)
            query = query.Where(e => e.ClientId == request.ClientId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(e => e.NextMaintenance ?? DateTime.MaxValue)
            .ThenBy(e => e.Client.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new EquipmentListItemDto(
                e.Id,
                e.Type,
                e.Brand,
                e.Model,
                e.SerialNumber,
                e.NextMaintenance,
                e.ClientId,
                e.Client.Name,
                e.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return Result.Success(new PaginatedResult<EquipmentListItemDto>(
            items, totalCount, request.Page, request.PageSize));
    }
}
