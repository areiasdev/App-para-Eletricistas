using Ardalis.Result;
using MediatR;
using TecnicoApp.Application.Common.DTOs;
using TecnicoApp.Application.Features.Clients.DTOs;

namespace TecnicoApp.Application.Features.Clients.Queries.GetClients;

public record GetClientsQuery(
    string? Search = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<PaginatedResult<ClientListItemDto>>>;
