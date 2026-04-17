using Ardalis.Result.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecnicoApp.Application.Features.Reports;

namespace TecnicoApp.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class ReportsController(IMediator mediator) : ControllerBase
{
    [HttpGet("profitability")]
    [ProducesResponseType(typeof(ProfitabilityReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProfitabilityReportDto>> GetProfitability(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetProfitabilityReportQuery(from, to), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToActionResult(this);
    }
}
