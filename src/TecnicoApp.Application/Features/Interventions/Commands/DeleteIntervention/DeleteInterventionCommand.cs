using Ardalis.Result;
using MediatR;

namespace TecnicoApp.Application.Features.Interventions.Commands.DeleteIntervention;

public record DeleteInterventionCommand(Guid Id) : IRequest<Result>;
