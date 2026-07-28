using Ardalis.Result.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecnicoApp.Application.Common.DTOs;
using TecnicoApp.Application.Features.Invoices.Commands.CreateInvoiceFromQuote;
using TecnicoApp.Application.Features.Invoices.Commands.UpdateInvoiceStatus;
using TecnicoApp.Application.Features.Invoices.DTOs;
using TecnicoApp.Application.Features.Invoices.Queries.GenerateInvoicePdf;
using TecnicoApp.Application.Features.Invoices.Queries.GetInvoiceById;
using TecnicoApp.Application.Features.Invoices.Queries.GetInvoices;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class InvoicesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<InvoiceListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<InvoiceListItemDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] InvoiceStatus? status,
        [FromQuery] Guid? clientId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new GetInvoicesQuery(page, pageSize, search, status, clientId), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToActionResult(this);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvoiceDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetInvoiceByIdQuery(id), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToActionResult(this);
    }

    [HttpPost("from-quote/{quoteId:guid}")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvoiceDto>> CreateFromQuote(Guid quoteId, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateInvoiceFromQuoteCommand(quoteId), ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : result.ToActionResult(this);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateInvoiceStatusRequest request,
        CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateInvoiceStatusCommand(id, request.Status), ct);
        return result.IsSuccess ? NoContent() : result.ToActionResult(this);
    }

    [HttpGet("{id:guid}/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPdf(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GenerateInvoicePdfQuery(id), ct);
        if (!result.IsSuccess)
            return result.Status switch
            {
                Ardalis.Result.ResultStatus.NotFound => NotFound(),
                Ardalis.Result.ResultStatus.Forbidden => Forbid(),
                _ => BadRequest()
            };

        var filename = $"fatura-{result.Value.Number}.pdf";
        return File(result.Value.Bytes, "application/pdf", filename);
    }
}

public record UpdateInvoiceStatusRequest(InvoiceStatus Status);
