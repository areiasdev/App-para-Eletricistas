using Ardalis.Result;
using MediatR;
using TecnicoApp.Application.Features.Interventions.DTOs;

namespace TecnicoApp.Application.Features.Interventions.Queries.GetInterventionById;

public record GetInterventionByIdQuery(Guid Id) : IRequest<Result<InterventionDto>>;
