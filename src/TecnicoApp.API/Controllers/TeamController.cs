using Ardalis.Result.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecnicoApp.Application.Features.Team.Commands.AcceptInvite;
using TecnicoApp.Application.Features.Team.Commands.InviteTeamMember;
using TecnicoApp.Application.Features.Team.Commands.RemoveTeamMember;
using TecnicoApp.Application.Features.Team.Commands.UpdateTeamMemberRole;
using TecnicoApp.Application.Features.Team.DTOs;
using TecnicoApp.Application.Features.Team.Queries.GetTeam;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class TeamController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TeamMemberDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TeamMemberDto>>> GetTeam(CancellationToken ct)
    {
        var result = await mediator.Send(new GetTeamQuery(), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToActionResult(this);
    }

    [HttpPost("invite")]
    [ProducesResponseType(typeof(TeamMemberDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TeamMemberDto>> Invite(
        [FromBody] InviteTeamMemberRequest request,
        CancellationToken ct)
    {
        var result = await mediator.Send(new InviteTeamMemberCommand(request.Email, request.Role), ct);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : result.ToActionResult(this);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new RemoveTeamMemberCommand(id), ct);
        return result.IsSuccess ? NoContent() : result.ToActionResult(this);
    }

    [HttpPatch("{id:guid}/role")]
    [ProducesResponseType(typeof(TeamMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamMemberDto>> UpdateRole(
        Guid id,
        [FromBody] UpdateRoleRequest request,
        CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateTeamMemberRoleCommand(id, request.Role), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToActionResult(this);
    }

    [HttpPost("accept-invite")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcceptInvite(
        [FromBody] AcceptInviteRequest request,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new AcceptInviteCommand(request.Token, request.FullName, request.NewPassword), ct);
        return result.IsSuccess ? Ok() : result.ToActionResult(this);
    }
}

public record InviteTeamMemberRequest(string Email, UserRole Role);
public record UpdateRoleRequest(UserRole Role);
public record AcceptInviteRequest(string Token, string FullName, string NewPassword);
