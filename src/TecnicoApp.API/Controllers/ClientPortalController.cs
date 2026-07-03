using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.API.Controllers;

[ApiController]
[Route("api/v1/portal")]
public class ClientPortalController(
    IAppDbContext db,
    ICurrentUserService currentUser,
    ITokenService tokenService,
    IEmailService emailService,
    IAppSettings appSettings) : ControllerBase
{
    // ── Tech-side: send magic link to client ─────────────────────────────────

    [HttpPost("send-access")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendAccess(
        [FromBody] SendPortalAccessRequest request,
        CancellationToken ct)
    {
        var userId = currentUser.UserId;

        var callingUser = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (callingUser is null) return Unauthorized();

        var ownerId = callingUser.OwnerId ?? callingUser.Id;

        // Verify plan allows client portal
        var ownerPlan = callingUser.OwnerId.HasValue
            ? await db.Users.AsNoTracking()
                .Where(u => u.Id == ownerId)
                .Select(u => u.Plan)
                .FirstOrDefaultAsync(ct)
            : callingUser.Plan;

        if (ownerPlan != Plan.Enterprise)
            return Problem(
                detail: "O portal do cliente está disponível no plano Enterprise.",
                statusCode: StatusCodes.Status403Forbidden);

        // Find client (must belong to this tenant)
        var client = await db.Clients
            .FirstOrDefaultAsync(c => c.Id == request.ClientId && c.UserId == ownerId, ct);

        if (client is null)
            return NotFound(new { detail = "Cliente não encontrado." });

        if (string.IsNullOrWhiteSpace(client.Email))
            return Problem(
                detail: "O cliente não tem email registado. Edita o cliente e adiciona um email.",
                statusCode: StatusCodes.Status400BadRequest);

        // Generate token
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');
        var tokenHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        client.PortalTokenHash = tokenHash;
        client.PortalTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        await db.SaveChangesAsync(ct);

        // Send email
        var portalUrl = $"{appSettings.BaseUrl}/portal/login?token={rawToken}";
        var html = $"""
            <div style="font-family:sans-serif;max-width:520px;margin:0 auto;color:#1a1a1a">
              <div style="margin-bottom:24px">
                <span style="background:#f59e0b;color:#1c1917;font-size:14px;font-weight:700;
                  padding:6px 10px;border-radius:6px">⚡ TécnicoApp</span>
              </div>
              <h1 style="font-size:22px;font-weight:700;margin-bottom:8px">
                O seu portal de cliente está disponível
              </h1>
              <p style="color:#555;margin-bottom:24px">
                Pode consultar os seus equipamentos, intervenções e orçamentos a qualquer momento.
                O acesso é válido por 7 dias.
              </p>
              <a href="{portalUrl}"
                style="display:inline-block;background:#f59e0b;color:#1c1917;font-weight:700;
                  font-size:15px;padding:12px 28px;border-radius:8px;text-decoration:none">
                Aceder ao portal →
              </a>
              <p style="color:#999;font-size:12px;margin-top:32px">
                Se não reconhece este email, pode ignorá-lo com segurança.
              </p>
            </div>
            """;

        await emailService.SendAsync(new EmailMessage(
            To: client.Email,
            ToName: client.Name,
            Subject: "Acesso ao seu portal de cliente — TécnicoApp",
            HtmlBody: html), ct);

        return Ok(new { message = $"Email enviado para {client.Email}." });
    }

    // ── Client-side: exchange token for JWT ───────────────────────────────────

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PortalLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(
        [FromBody] PortalLoginRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return Problem(detail: "Token inválido.", statusCode: 400);

        var tokenHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));

        var client = await db.Clients
            .FirstOrDefaultAsync(c => c.PortalTokenHash == tokenHash, ct);

        if (client is null || client.PortalTokenExpiresAt < DateTime.UtcNow)
            return Problem(
                detail: "Link de acesso inválido ou expirado. Pede ao teu técnico um novo link.",
                statusCode: StatusCodes.Status400BadRequest);

        var accessToken = tokenService.GeneratePortalToken(
            client.Id, client.UserId, client.Name, client.Email ?? string.Empty);

        return Ok(new PortalLoginResponse(accessToken, client.Name, client.Email));
    }

    // ── Portal endpoints (ClientPortal JWT required) ───────────────────────────

    [HttpGet("me")]
    [Authorize(Roles = "ClientPortal")]
    [ProducesResponseType(typeof(PortalClientDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var (clientId, _) = GetPortalClaims();

        var client = await db.Clients.AsNoTracking()
            .Where(c => c.Id == clientId)
            .Select(c => new PortalClientDto(
                c.Id, c.Name, c.Email, c.Phone,
                c.Address != null ? $"{c.Address.Street}, {c.Address.PostalCode} {c.Address.City}" : null,
                c.Notes))
            .FirstOrDefaultAsync(ct);

        if (client is null) return NotFound();
        return Ok(client);
    }

    [HttpGet("equipment")]
    [Authorize(Roles = "ClientPortal")]
    [ProducesResponseType(typeof(IReadOnlyList<PortalEquipmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Equipment(CancellationToken ct)
    {
        var (clientId, _) = GetPortalClaims();

        var items = await db.Equipment.AsNoTracking()
            .Where(e => e.ClientId == clientId)
            .OrderBy(e => e.Type)
            .Select(e => new PortalEquipmentDto(
                e.Id, e.Type, e.Brand, e.Model, e.SerialNumber,
                e.InstalledAt, e.NextMaintenance, e.Notes))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("interventions")]
    [Authorize(Roles = "ClientPortal")]
    [ProducesResponseType(typeof(IReadOnlyList<PortalInterventionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Interventions(CancellationToken ct)
    {
        var (clientId, _) = GetPortalClaims();

        var assignedIds = await db.Interventions.AsNoTracking()
            .Where(i => i.ClientId == clientId && i.AssignedToUserId != null)
            .Select(i => i.AssignedToUserId!.Value)
            .Distinct()
            .ToListAsync(ct);

        var techNames = await db.Users.AsNoTracking()
            .Where(u => assignedIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName })
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        var rawItems = await db.Interventions.AsNoTracking()
            .Where(i => i.ClientId == clientId)
            .OrderByDescending(i => i.ScheduledAt)
            .Take(50)
            .Select(i => new
            {
                i.Id, i.Status, i.Title,
                i.Description, i.ScheduledAt, i.CompletedAt, i.AssignedToUserId
            })
            .ToListAsync(ct);

        var items = rawItems.Select(i => new PortalInterventionDto(
            i.Id, i.Title, i.Status.ToString(),
            i.Description, i.ScheduledAt, i.CompletedAt,
            i.AssignedToUserId.HasValue && techNames.TryGetValue(i.AssignedToUserId.Value, out var name) ? name : null))
            .ToList();

        return Ok(items);
    }

    [HttpGet("quotes")]
    [Authorize(Roles = "ClientPortal")]
    [ProducesResponseType(typeof(IReadOnlyList<PortalQuoteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Quotes(CancellationToken ct)
    {
        var (clientId, _) = GetPortalClaims();

        var items = await db.Quotes.AsNoTracking()
            .Where(q => q.ClientId == clientId)
            .OrderByDescending(q => q.CreatedAt)
            .Take(20)
            .Select(q => new PortalQuoteDto(
                q.Id, q.Number, q.Status.ToString(),
                q.Total, q.ValidUntil, q.CreatedAt))
            .ToListAsync(ct);

        return Ok(items);
    }

    private (Guid ClientId, Guid OwnerId) GetPortalClaims()
    {
        var clientId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ownerId  = Guid.Parse(User.FindFirstValue("ownerId")!);
        return (clientId, ownerId);
    }
}

public record SendPortalAccessRequest(Guid ClientId);
public record PortalLoginRequest(string Token);
public record PortalLoginResponse(string AccessToken, string ClientName, string? ClientEmail);
public record PortalClientDto(Guid Id, string Name, string? Email, string? Phone, string? Address, string? Notes);
public record PortalEquipmentDto(Guid Id, string Type, string? Brand, string? Model, string? SerialNumber, DateTime? InstalledAt, DateTime? NextMaintenance, string? Notes);
public record PortalInterventionDto(Guid Id, string Title, string Status, string? Description, DateTime? ScheduledAt, DateTime? CompletedAt, string? TechnicianName);
public record PortalQuoteDto(Guid Id, string Number, string Status, decimal Total, DateTime? ValidUntil, DateTime CreatedAt);
