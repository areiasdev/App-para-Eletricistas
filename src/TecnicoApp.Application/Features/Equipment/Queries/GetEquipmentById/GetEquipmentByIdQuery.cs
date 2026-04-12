using Ardalis.Result;
using MediatR;
using TecnicoApp.Application.Features.Equipment.DTOs;

namespace TecnicoApp.Application.Features.Equipment.Queries.GetEquipmentById;

public record GetEquipmentByIdQuery(Guid Id) : IRequest<Result<EquipmentDto>>;
