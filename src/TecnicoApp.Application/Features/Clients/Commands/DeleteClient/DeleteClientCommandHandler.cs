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

        client.IsDeleted = true;
        client.ModifiedBy = currentUser.Email;
        client.ModifiedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
