using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;

namespace TecnicoApp.Application.Features.Clients.Commands.DeleteClient;

public sealed class DeleteClientCommandHandler(
    IAppDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<DeleteClientCommand, Result>
{
    public async Task<Result> Handle(
        DeleteClientCommand command,
        CancellationToken cancellationToken)
    {
        var client = await db.Clients
            .FirstOrDefaultAsync(
                c => c.Id == command.ClientId && c.UserId == currentUser.UserId,
                cancellationToken);

        if (client is null)
            return Result.NotFound("Cliente não encontrado.");

        var now = DateTime.UtcNow;
        var email = currentUser.Email;

        client.IsDeleted = true;
        client.ModifiedBy = email;
        client.ModifiedAt = now;

        // Cascade soft-delete to equipment and interventions belonging to this client
        var clientEquipment = await db.Equipment
            .Where(e => e.ClientId == command.ClientId)
            .ToListAsync(cancellationToken);

        foreach (var eq in clientEquipment)
        {
            eq.IsDeleted = true;
            eq.ModifiedAt = now;
            eq.ModifiedBy = email;
        }

        var clientInterventions = await db.Interventions
            .Where(i => i.ClientId == command.ClientId)
            .ToListAsync(cancellationToken);

        foreach (var iv in clientInterventions)
        {
            iv.IsDeleted = true;
            iv.ModifiedAt = now;
            iv.ModifiedBy = email;
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
