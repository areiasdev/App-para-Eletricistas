using Ardalis.Result;
using MediatR;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Interventions.Commands.UpdateInterventionStatus;

public record UpdateInterventionStatusCommand(Guid Id, InterventionStatus Status) : IRequest<Result>;
