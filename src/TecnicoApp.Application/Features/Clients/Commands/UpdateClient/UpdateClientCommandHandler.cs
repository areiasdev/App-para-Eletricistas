using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Clients.DTOs;
using TecnicoApp.Domain.ValueObjects;
using TecnicoApp.Application.Common.Extensions;

namespace TecnicoApp.Application.Features.Clients.Commands.UpdateClient;

public sealed class UpdateClientCommandHandler(
    IAppDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<UpdateClientCommand, Result<ClientDto>>
{
    public async Task<Result<ClientDto>> Handle(
        UpdateClientCommand command,
        CancellationToken cancellationToken)
    {
        // Resolve ownerId: team members share their owner's clients
        var ownerId = await db.ResolveOwnerIdAsync(currentUser.UserId, cancellationToken);

        var client = await db.Clients
            .FirstOrDefaultAsync(
                c => c.Id == command.ClientId && c.UserId == ownerId,
                cancellationToken);

        if (client is null)
            return Result.NotFound("Cliente não encontrado.");

        client.Name = command.Name;
        client.Nif = command.Nif;
        client.Email = command.Email?.ToLowerInvariant();
        client.Phone = command.Phone;
        client.Notes = command.Notes;
        client.ModifiedBy = currentUser.Email;
        client.WhatsAppOptIn = command.WhatsAppOptIn;
        client.PhoneVerified = command.PhoneVerified;
        client.Address = command.Address is null ? null : new Address(
            command.Address.Street,
            command.Address.City,
            command.Address.PostalCode,
            command.Address.Country);

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
            client.CreatedAt,
            client.WhatsAppOptIn,
            client.PhoneVerified));
    }
}
