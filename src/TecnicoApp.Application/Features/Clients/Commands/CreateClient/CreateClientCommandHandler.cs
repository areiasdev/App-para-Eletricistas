using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Clients.DTOs;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.ValueObjects;

namespace TecnicoApp.Application.Features.Clients.Commands.CreateClient;

public sealed class CreateClientCommandHandler(
    IAppDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<CreateClientCommand, Result<ClientDto>>
{
    public async Task<Result<ClientDto>> Handle(
        CreateClientCommand command,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        // Resolve ownerId: team members share their owner's clients
        var ownerId = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.OwnerId ?? u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (ownerId == Guid.Empty) return Result.Unauthorized();

        var ownerExists = await db.Users.AsNoTracking()
            .AnyAsync(u => u.Id == ownerId, cancellationToken);

        if (!ownerExists) return Result.Unauthorized();

        var client = new Client
        {
            Name = command.Name,
            Nif = command.Nif,
            Email = command.Email?.ToLowerInvariant(),
            Phone = command.Phone,
            Notes = command.Notes,
            UserId = ownerId,
            ModifiedBy = currentUser.Email,
            Address = command.Address is null ? null : new Address(
                command.Address.Street,
                command.Address.City,
                command.Address.PostalCode,
                command.Address.Country)
        };

        await db.Clients.AddAsync(client, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new ClientDto(
            client.Id,
            client.Name,
            client.Nif,
            client.Email,
            client.Phone,
            client.Address is null ? null : new AddressDto(
                client.Address.Street,
                client.Address.City,
                client.Address.PostalCode,
                client.Address.Country),
            client.Notes,
            client.CreatedAt));
    }
}
