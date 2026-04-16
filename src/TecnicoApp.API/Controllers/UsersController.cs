using Ardalis.Result.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecnicoApp.Application.Features.Users.Commands.UpdateProfile;
using TecnicoApp.Application.Features.Users.DTOs;
using TecnicoApp.Application.Features.Users.Queries.GetProfile;

namespace TecnicoApp.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class UsersController(IMediator mediator) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileDto>> GetProfile(CancellationToken ct)
    {
        var result = await mediator.Send(new GetProfileQuery(), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToActionResult(this);
    }

    [HttpPut("me")]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileDto>> UpdateProfile(
        [FromBody] UpdateProfileCommand command,
        CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToActionResult(this);
    }
}
