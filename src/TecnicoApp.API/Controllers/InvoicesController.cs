using Ardalis.Result.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecnicoApp.Application.Common.DTOs;
using TecnicoApp.Application.Features.Invoices.Commands.CreateInvoiceCheckout;
using TecnicoApp.Application.Features.Invoices.Commands.CreateInvoiceFromQuote;
using TecnicoApp.Application.Features.Invoices.Commands.GetOrCreateInvoicePayLink;
using TecnicoApp.Application.Features.Invoices.Commands.SendInvoiceEmail;
using TecnicoApp.Application.Features.Invoices.Commands.UpdateInvoiceStatus;
using TecnicoApp.Application.Features.Invoices.Commands.CreateInvoiceFromIntervention;
using TecnicoApp.Application.Features.Invoices.DTOs;
using TecnicoApp.Application.Features.Invoices.Queries.ExportInvoices;
using TecnicoApp.Application.Features.Invoices.Queries.GenerateInvoicePdf;
using TecnicoApp.Application.Features.Invoices.Queries.GetInvoiceById;
using TecnicoApp.Application.Features.Invoices.Queries.GetInvoices;
using TecnicoApp.Application.Features.Invoices.Queries.GetPublicInvoiceByToken;
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

    [HttpGet("export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportCsv([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var result = await mediator.Send(new ExportInvoicesCsvQuery(from, to), ct);
        if (!result.IsSuccess) return result.ToActionResult(this).Result!;
        var name = $"faturas-{(from ?? new DateTime(DateTime.UtcNow.Year, 1, 1)):yyyy-MM-dd}-a-{(to ?? DateTime.UtcNow):yyyy-MM-dd}.csv";
        return File(result.Value, "text/csv; charset=utf-8", name);
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

    [HttpPost("from-intervention/{interventionId:guid}")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InvoiceDto>> CreateFromIntervention(Guid interventionId, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateInvoiceFromInterventionCommand(interventionId), ct);
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

    [HttpPost("{id:guid}/pay-link")]
    [ProducesResponseType(typeof(PayLinkResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PayLinkResponse>> GetPayLink(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetOrCreateInvoicePayLinkCommand(id), ct);
        if (!result.IsSuccess)
            return result.Status switch
            {
                Ardalis.Result.ResultStatus.NotFound => NotFound(),
                Ardalis.Result.ResultStatus.Forbidden => Forbid(),
                Ardalis.Result.ResultStatus.Unauthorized => Unauthorized(),
                _ => BadRequest()
            };
        return Ok(new PayLinkResponse(result.Value));
    }

    [HttpPost("{id:guid}/send-email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendEmail(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new SendInvoiceEmailCommand(id), ct);
        return result.IsSuccess ? NoContent() : result.ToActionResult(this);
    }

    // ── Public/unauthenticated — reached only via the invoice's magic-link pay token ──────────

    [HttpGet("public/{token}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PublicInvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicInvoiceDto>> GetPublicByToken(string token, CancellationToken ct)
    {
        var result = await mediator.Send(new GetPublicInvoiceByTokenQuery(token), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToActionResult(this);
    }

    [HttpPost("public/{token}/checkout")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PayLinkResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PayLinkResponse>> CreatePublicCheckout(string token, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateInvoiceCheckoutCommand(token), ct);
        if (!result.IsSuccess)
            return result.Status switch
            {
                Ardalis.Result.ResultStatus.NotFound => NotFound(),
                _ => BadRequest(new { detail = result.Errors.FirstOrDefault() ?? "Não foi possível iniciar o pagamento." })
            };
        return Ok(new PayLinkResponse(result.Value));
    }
}

public record UpdateInvoiceStatusRequest(InvoiceStatus Status);
public record PayLinkResponse(string Url);
