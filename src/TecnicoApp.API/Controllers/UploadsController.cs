using Ardalis.Result.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecnicoApp.Application.Features.Uploads;

namespace TecnicoApp.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class UploadsController(IMediator mediator) : ControllerBase
{
    [HttpPost("photos")]
    [RequestSizeLimit(UploadPhotoCommandValidator.MaxSizeBytes + 64 * 1024)]
    [ProducesResponseType(typeof(UploadedFileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UploadedFileResponse>> UploadPhoto(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return Problem(detail: "Ficheiro em falta.", statusCode: StatusCodes.Status400BadRequest);

        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, ct);

        var result = await mediator.Send(new UploadPhotoCommand(stream.ToArray(), file.ContentType), ct);
        return result.IsSuccess ? Ok(new UploadedFileResponse(result.Value)) : result.ToActionResult(this).Result!;
    }
}

public record UploadedFileResponse(string Url);
