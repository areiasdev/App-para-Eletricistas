using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;

namespace TecnicoApp.Application.Features.Equipment.Commands.DeleteEquipment;

public class DeleteEquipmentCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<DeleteEquipmentCommand, Result>
{
    public async Task<Result> Handle(
        DeleteEquipmentCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var equipment = await db.Equipment
            .Include(e => e.Client)
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (equipment is null)
            return Result.NotFound();

        if (equipment.Client.UserId != userId)
            return Result.Forbidden();

        equipment.IsDeleted = true;
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
