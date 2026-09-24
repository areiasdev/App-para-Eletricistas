using Ardalis.Result.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecnicoApp.Application.Common.DTOs;
using TecnicoApp.Application.Features.Interventions.Commands.CreateIntervention;
using TecnicoApp.Application.Features.Interventions.Commands.DeleteIntervention;
using TecnicoApp.Application.Features.Interventions.Commands.UpdateIntervention;
using TecnicoApp.Application.Features.Interventions.Commands.UpdateInterventionStatus;
using TecnicoApp.Application.Features.Interventions.Commands.SignIntervention;
using TecnicoApp.Application.Features.Interventions.DTOs;
using TecnicoApp.Application.Features.Interventions.Queries.GenerateInterventionReport;
using TecnicoApp.Application.Features.Interventions.Queries.GetInterventionById;
using TecnicoApp.Application.Features.Interventions.Queries.GetInterventions;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class InterventionsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<InterventionListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<InterventionListItemDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] InterventionStatus? status,
        [FromQuery] Guid? clientId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? assignedToUserId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new GetInterventionsQuery(page, pageSize, search, status, clientId, from, to, assignedToUserId), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToActionResult(this);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(InterventionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InterventionDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetInterventionByIdQuery(id), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToActionResult(this);
    }

    [HttpPost]
    [ProducesResponseType(typeof(InterventionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InterventionDto>> Create(
        [FromBody] CreateInterventionCommand command,
        CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : result.ToActionResult(this);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(InterventionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InterventionDto>> Update(
        Guid id,
        [FromBody] UpdateInterventionRequest request,
        CancellationToken ct)
    {
        var command = new UpdateInterventionCommand(
            id, request.Title, request.Description, request.ScheduledAt,
            request.TechnicianNotes, request.QuoteId, request.EquipmentIds, request.Photos,
            request.Materials, request.AssignedToUserId, request.LaborHours);

        var result = await mediator.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToActionResult(this);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateInterventionStatusRequest request,
        CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateInterventionStatusCommand(id, request.Status), ct);
        return result.IsSuccess ? NoContent() : result.ToActionResult(this);
    }

    [HttpPost("{id:guid}/sign")]
    [ProducesResponseType(typeof(InterventionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InterventionDto>> Sign(
        Guid id, [FromBody] SignInterventionRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new SignInterventionCommand(id, request.SignedByName, request.SignatureDataUrl), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToActionResult(this);
    }

    [HttpGet("{id:guid}/report")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadReport(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GenerateInterventionReportQuery(id), ct);
        return result.IsSuccess
            ? File(result.Value.Bytes, "application/pdf", result.Value.FileName)
            : result.ToActionResult(this).Result!;
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteInterventionCommand(id), ct);
        return result.IsSuccess ? NoContent() : result.ToActionResult(this);
    }
}

public record UpdateInterventionRequest(
    string Title,
    string? Description,
    DateTime? ScheduledAt,
    string? TechnicianNotes,
    Guid? QuoteId,
    IReadOnlyList<Guid> EquipmentIds,
    IReadOnlyList<string>? Photos,
    IReadOnlyList<TecnicoApp.Application.Features.Interventions.Commands.CreateIntervention.InterventionMaterialRequest>? Materials,
    Guid? AssignedToUserId,
    decimal? LaborHours = null
);

public record UpdateInterventionStatusRequest(InterventionStatus Status);

public record SignInterventionRequest(string SignedByName, string SignatureDataUrl);
