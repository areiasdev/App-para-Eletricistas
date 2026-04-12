using Ardalis.Result;
using MediatR;

namespace TecnicoApp.Application.Features.Equipment.Commands.DeleteEquipment;

public record DeleteEquipmentCommand(Guid Id) : IRequest<Result>;
