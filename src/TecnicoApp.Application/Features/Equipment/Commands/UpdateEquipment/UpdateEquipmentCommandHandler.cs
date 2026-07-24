using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Equipment.DTOs;

namespace TecnicoApp.Application.Features.Equipment.Commands.UpdateEquipment;

public class UpdateEquipmentCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpdateEquipmentCommand, Result<EquipmentDto>>
{
    public async Task<Result<EquipmentDto>> Handle(
        UpdateEquipmentCommand request, CancellationToken cancellationToken)
    {
        // Resolve ownerId: team members share their owner's clients/equipment
        var ownerId = await db.Users.AsNoTracking()
            .Where(u => u.Id == currentUser.UserId)
            .Select(u => u.OwnerId ?? u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var equipment = await db.Equipment
            .Include(e => e.Client)
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (equipment is null)
            return Result.NotFound();

        if (equipment.Client.UserId != ownerId)
            return Result.Forbidden();

        equipment.Type = request.Type;
        equipment.Brand = request.Brand;
        equipment.Model = request.Model;
        equipment.SerialNumber = request.SerialNumber;
        equipment.InstalledAt = request.InstalledAt;
        equipment.NextMaintenance = request.NextMaintenance;
        equipment.Notes = request.Notes;
        equipment.Photos = request.Photos?.ToList() ?? equipment.Photos;
        equipment.ModifiedBy = currentUser.Email;

        await db.SaveChangesAsync(cancellationToken);

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
