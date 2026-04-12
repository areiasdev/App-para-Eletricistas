using Ardalis.Result;
using MediatR;
using TecnicoApp.Application.Common.DTOs;
using TecnicoApp.Application.Features.Equipment.DTOs;

namespace TecnicoApp.Application.Features.Equipment.Queries.GetEquipment;

public record GetEquipmentQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    Guid? ClientId = null
) : IRequest<Result<PaginatedResult<EquipmentListItemDto>>>;
