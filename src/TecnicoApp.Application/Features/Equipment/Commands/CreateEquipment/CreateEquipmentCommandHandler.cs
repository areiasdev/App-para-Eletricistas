using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Equipment.DTOs;

namespace TecnicoApp.Application.Features.Equipment.Commands.CreateEquipment;

public class CreateEquipmentCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CreateEquipmentCommand, Result<EquipmentDto>>
{
    public async Task<Result<EquipmentDto>> Handle(
        CreateEquipmentCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var client = await db.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ClientId, cancellationToken);

        if (client is null)
            return Result.NotFound("Cliente não encontrado.");

        if (client.UserId != userId)
            return Result.Forbidden();

        var equipment = new Domain.Entities.Equipment
        {
            Type = request.Type,
            Brand = request.Brand,
            Model = request.Model,
            SerialNumber = request.SerialNumber,
            InstalledAt = request.InstalledAt,
            NextMaintenance = request.NextMaintenance,
            Notes = request.Notes,
            ClientId = request.ClientId,
        };

        db.Equipment.Add(equipment);
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
            client.Name,
            equipment.CreatedAt
        ));
    }
}
