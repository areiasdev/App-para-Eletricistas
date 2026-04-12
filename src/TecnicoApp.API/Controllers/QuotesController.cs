using Ardalis.Result.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecnicoApp.Application.Common.DTOs;
using TecnicoApp.Application.Features.Quotes.Commands.CreateQuote;
using TecnicoApp.Application.Features.Quotes.Commands.DeleteQuote;
using TecnicoApp.Application.Features.Quotes.Commands.UpdateQuote;
using TecnicoApp.Application.Features.Quotes.Commands.UpdateQuoteStatus;
using TecnicoApp.Application.Features.Quotes.DTOs;
using TecnicoApp.Application.Features.Quotes.Queries.GenerateQuotePdf;
using TecnicoApp.Application.Features.Quotes.Queries.GetQuoteById;
using TecnicoApp.Application.Features.Quotes.Queries.GetQuotes;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class QuotesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<QuoteListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<QuoteListItemDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] QuoteStatus? status,
        [FromQuery] Guid? clientId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new GetQuotesQuery(page, pageSize, search, status, clientId), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToActionResult(this);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(QuoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuoteDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetQuoteByIdQuery(id), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToActionResult(this);
    }

    [HttpPost]
    [ProducesResponseType(typeof(QuoteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<QuoteDto>> Create(
        [FromBody] CreateQuoteCommand command,
        CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : result.ToActionResult(this);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(QuoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<QuoteDto>> Update(
        Guid id,
        [FromBody] UpdateQuoteRequest request,
        CancellationToken ct)
    {
        var command = new UpdateQuoteCommand(
            id, request.ClientId, request.Discount, request.Notes,
            request.ValidUntil, request.Lines);

        var result = await mediator.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToActionResult(this);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateQuoteStatusRequest request,
        CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateQuoteStatusCommand(id, request.Status), ct);
        return result.IsSuccess ? NoContent() : result.ToActionResult(this);
    }

    [HttpGet("{id:guid}/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPdf(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GenerateQuotePdfQuery(id), ct);
        if (!result.IsSuccess)
            return result.Status switch
            {
                Ardalis.Result.ResultStatus.NotFound => NotFound(),
                Ardalis.Result.ResultStatus.Forbidden => Forbid(),
                _ => BadRequest()
            };

        var filename = $"orcamento-{result.Value.Number}.pdf";
        return File(result.Value.Bytes, "application/pdf", filename);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteQuoteCommand(id), ct);
        return result.IsSuccess ? NoContent() : result.ToActionResult(this);
    }
}

public record UpdateQuoteRequest(
    Guid ClientId,
    decimal? Discount,
    string? Notes,
    DateTime? ValidUntil,
    IReadOnlyList<CreateQuoteLineRequest> Lines
);

public record UpdateQuoteStatusRequest(QuoteStatus Status);
