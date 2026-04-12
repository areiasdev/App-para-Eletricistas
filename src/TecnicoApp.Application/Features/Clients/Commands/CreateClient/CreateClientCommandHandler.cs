using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Clients.DTOs;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
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

        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null) return Result.Unauthorized();

        if (user.Plan == Plan.Free)
        {
            var count = await db.Clients
                .CountAsync(c => c.UserId == userId, cancellationToken);

            if (count >= 5)
                return Result.Invalid(new ValidationError(
                    "PlanLimit",
                    "Atingiste o limite de 5 clientes do plano Free. Faz upgrade para Pro para clientes ilimitados.",
                    "PLAN_LIMIT_CLIENTS",
                    ValidationSeverity.Error));
        }

        var client = new Client
        {
            Name = command.Name,
            Nif = command.Nif,
            Email = command.Email?.ToLowerInvariant(),
            Phone = command.Phone,
            Notes = command.Notes,
            UserId = userId,
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
