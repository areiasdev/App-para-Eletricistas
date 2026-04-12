using Ardalis.Result;
using MediatR;
using TecnicoApp.Application.Common.DTOs;
using TecnicoApp.Application.Features.Interventions.DTOs;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Interventions.Queries.GetInterventions;

public record GetInterventionsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    InterventionStatus? Status = null,
    Guid? ClientId = null
) : IRequest<Result<PaginatedResult<InterventionListItemDto>>>;
