using Ardalis.Result.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecnicoApp.Application.Common.DTOs;
using TecnicoApp.Application.Features.Equipment.Commands.CreateEquipment;
using TecnicoApp.Application.Features.Equipment.Commands.DeleteEquipment;
using TecnicoApp.Application.Features.Equipment.Commands.UpdateEquipment;
using TecnicoApp.Application.Features.Equipment.DTOs;
using TecnicoApp.Application.Features.Equipment.Queries.GetEquipment;
using TecnicoApp.Application.Features.Equipment.Queries.GetEquipmentById;

namespace TecnicoApp.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class EquipmentController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<EquipmentListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<EquipmentListItemDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] Guid? clientId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new GetEquipmentQuery(page, pageSize, search, clientId), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToActionResult(this);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EquipmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EquipmentDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetEquipmentByIdQuery(id), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToActionResult(this);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EquipmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EquipmentDto>> Create(
        [FromBody] CreateEquipmentCommand command,
        CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : result.ToActionResult(this);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EquipmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EquipmentDto>> Update(
        Guid id,
        [FromBody] UpdateEquipmentRequest request,
        CancellationToken ct)
    {
        var command = new UpdateEquipmentCommand(
            id, request.Type, request.Brand, request.Model, request.SerialNumber,
            request.InstalledAt, request.NextMaintenance, request.Notes);

        var result = await mediator.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToActionResult(this);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteEquipmentCommand(id), ct);
        return result.IsSuccess ? NoContent() : result.ToActionResult(this);
    }
}

public record UpdateEquipmentRequest(
    string Type,
    string? Brand,
    string? Model,
    string? SerialNumber,
    DateTime? InstalledAt,
    DateTime? NextMaintenance,
    string? Notes
);
