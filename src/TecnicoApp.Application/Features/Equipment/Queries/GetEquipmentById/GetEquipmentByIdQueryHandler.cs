using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Equipment.DTOs;

namespace TecnicoApp.Application.Features.Equipment.Queries.GetEquipmentById;

public class GetEquipmentByIdQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetEquipmentByIdQuery, Result<EquipmentDto>>
{
    public async Task<Result<EquipmentDto>> Handle(
        GetEquipmentByIdQuery request, CancellationToken cancellationToken)
    {
        // Resolve ownerId: team members share their owner's clients/equipment
        var ownerId = await db.Users.AsNoTracking()
            .Where(u => u.Id == currentUser.UserId)
            .Select(u => u.OwnerId ?? u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var equipment = await db.Equipment
            .AsNoTracking()
            .Include(e => e.Client)
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (equipment is null)
            return Result.NotFound();

        if (equipment.Client.UserId != ownerId)
            return Result.Forbidden();

        return Result.Success(new EquipmentDto(
            equipment.Id,
            equipment.Type,
            equipment.Brand,
            equipment.Model,
            equipment.SerialNumber,
            equipment.InstalledAt,
            equipment.NextMaintenance,
            equipment.Notes,
            equipment.Photos,
            equipment.ClientId,
            equipment.Client.Name,
            equipment.CreatedAt
        ));
    }
}
