using Ardalis.Result.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecnicoApp.Application.Features.Dashboard;

namespace TecnicoApp.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class DashboardController(IMediator mediator) : ControllerBase
{
    [HttpGet("stats")]
    [ProducesResponseType(typeof(DashboardStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardStatsDto>> GetStats(CancellationToken ct)
    {
        var result = await mediator.Send(new GetDashboardStatsQuery(), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToActionResult(this);
    }
}
