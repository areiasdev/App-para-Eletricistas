using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Equipment.DTOs;
using TecnicoApp.Application.Common.Extensions;

namespace TecnicoApp.Application.Features.Equipment.Commands.CreateEquipment;

public class CreateEquipmentCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CreateEquipmentCommand, Result<EquipmentDto>>
{
    public async Task<Result<EquipmentDto>> Handle(
        CreateEquipmentCommand request, CancellationToken cancellationToken)
    {
        // Resolve ownerId: team members share their owner's clients/equipment
        var ownerId = await db.ResolveOwnerIdAsync(currentUser.UserId, cancellationToken);

        var client = await db.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ClientId, cancellationToken);

        if (client is null)
            return Result.NotFound("Cliente não encontrado.");

        if (client.UserId != ownerId)
            return Result.Forbidden();

        var equipment = new Domain.Entities.Equipment
        {
            Type = request.Type,
            Brand = request.Brand,
            Model = request.Model,
            SerialNumber = request.SerialNumber,
            InstalledAt = request.InstalledAt,
            NextMaintenance = request.NextMaintenance,
            MaintenanceIntervalMonths = request.MaintenanceIntervalMonths,
            Notes = request.Notes,
            ClientId = request.ClientId,
            Photos = request.Photos?.ToList() ?? [],
        };

        db.Equipment.Add(equipment);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(equipment.ToDto(client.Name));
    }
}
