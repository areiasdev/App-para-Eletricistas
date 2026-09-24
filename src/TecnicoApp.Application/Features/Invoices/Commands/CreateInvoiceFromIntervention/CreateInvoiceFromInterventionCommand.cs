using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecnicoApp.Application.Common.Extensions;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Invoices.DTOs;
using TecnicoApp.Domain.Common;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Invoices.Commands.CreateInvoiceFromIntervention;

/// <summary>
/// Bills a completed job directly — hours at the company's hourly rate plus the materials used —
/// for the many service calls (avarias, pequenas reparações) that never had a quote.
/// </summary>
public record CreateInvoiceFromInterventionCommand(Guid InterventionId) : IRequest<Result<InvoiceDto>>;

public class CreateInvoiceFromInterventionCommandHandler(
    IAppDbContext db,
    ICurrentUserService currentUser,
    IAppSettings appSettings,
    ILogger<CreateInvoiceFromInterventionCommandHandler> logger)
    : IRequestHandler<CreateInvoiceFromInterventionCommand, Result<InvoiceDto>>
{
    public async Task<Result<InvoiceDto>> Handle(CreateInvoiceFromInterventionCommand request, CancellationToken cancellationToken)
    {
        var caller = await db.ResolveCallerAsync(currentUser.UserId, cancellationToken);
        if (caller is null) return Result.Unauthorized();
        if (caller.Role is not (UserRole.Owner or UserRole.Admin))
            return Result.Forbidden("Apenas o proprietário ou administradores podem faturar intervenções.");

        var intervention = await db.Interventions
            .Include(i => i.Client)
            .Include(i => i.Quote)
            .FirstOrDefaultAsync(i => i.Id == request.InterventionId, cancellationToken);

        if (intervention is null) return Result.NotFound();
        if (intervention.UserId != caller.OwnerId) return Result.Forbidden();

        if (intervention.Status != InterventionStatus.Completed)
            return Result.Error("Só é possível faturar intervenções concluídas.");

        if (intervention.Quote is { Status: QuoteStatus.Invoiced })
            return Result.Error("Esta intervenção pertence a um orçamento que já foi faturado.");

        var alreadyInvoiced = await db.Invoices.AnyAsync(
            i => i.InterventionId == intervention.Id && i.Status != InvoiceStatus.Cancelled, cancellationToken);
        if (alreadyInvoiced)
            return Result.Error("Esta intervenção já foi faturada.");

        var owner = await db.Users.AsNoTracking().FirstAsync(u => u.Id == caller.OwnerId, cancellationToken);

        var lines = new List<InvoiceLine>();
        if (intervention.LaborHours is > 0)
        {
            if (owner.DefaultHourlyRate is null)
                return Result.Error("Define o preço/hora da mão de obra no Perfil antes de faturar horas.");

            lines.Add(new InvoiceLine
            {
                Description = $"Mão de obra — {intervention.Title}",
                Quantity = intervention.LaborHours.Value,
                Unit = "h",
                UnitPrice = owner.DefaultHourlyRate.Value,
            });
        }

        lines.AddRange(intervention.Materials.Select(m => new InvoiceLine
        {
            Description = m.Name,
            Quantity = m.Quantity,
            UnitPrice = m.UnitPrice ?? m.UnitCost,
            Unit = DocumentMath.DefaultUnit,
        }));

        if (lines.Count == 0)
            return Result.Error("A intervenção não tem horas nem materiais para faturar.");

        for (var i = 0; i < lines.Count; i++)
            lines[i].Position = i;

        var issuedAt = DateTime.UtcNow;
        var invoice = new Invoice
        {
            Number = string.Empty, // assigned by AddInvoiceWithNextNumberAsync
            IssuedAt = issuedAt,
            DueDate = issuedAt.AddDays(appSettings.InvoicePaymentTermDays),
            Notes = $"Intervenção: {intervention.Title}" +
                    (intervention.CompletedAt is { } done ? $" (concluída a {done:dd/MM/yyyy})" : ""),
            InterventionId = intervention.Id,
            ClientId = intervention.ClientId,
            UserId = caller.OwnerId,
            Lines = lines,
        };

        await db.AddInvoiceWithNextNumberAsync(invoice, caller.OwnerId, logger, cancellationToken);
        return Result.Success(invoice.ToDto(intervention.Client.Name));
    }
}
