using Ardalis.Result.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecnicoApp.Application.Features.Users.Commands.UpdateProfile;
using TecnicoApp.Application.Features.Users.Commands.UploadLogo;
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

    [HttpPost("me/logo")]
    [RequestSizeLimit(2 * 1024 * 1024)]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProfileDto>> UploadLogo(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return Problem(detail: "Ficheiro em falta.", statusCode: StatusCodes.Status400BadRequest);

        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, ct);

        var result = await mediator.Send(new UploadLogoCommand(stream.ToArray(), file.ContentType), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToActionResult(this);
    }
}
