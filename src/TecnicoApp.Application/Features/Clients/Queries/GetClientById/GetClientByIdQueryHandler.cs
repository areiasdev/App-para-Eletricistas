using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Clients.DTOs;

namespace TecnicoApp.Application.Features.Clients.Queries.GetClientById;

public sealed class GetClientByIdQueryHandler(
    IAppDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetClientByIdQuery, Result<ClientDto>>
{
    public async Task<Result<ClientDto>> Handle(
        GetClientByIdQuery query,
        CancellationToken cancellationToken)
    {
        var client = await db.Clients
            .AsNoTracking()
            .Where(c => c.Id == query.ClientId && c.UserId == currentUser.UserId)
            .Select(c => new ClientDto(
                c.Id,
                c.Name,
                c.Nif,
                c.Email,
                c.Phone,
                c.Address == null ? null : new AddressDto(
                    c.Address.Street,
                    c.Address.City,
                    c.Address.PostalCode,
                    c.Address.Country),
                c.Notes,
                c.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (client is null)
            return Result.NotFound("Cliente não encontrado.");

        return Result.Success(client);
    }
}
