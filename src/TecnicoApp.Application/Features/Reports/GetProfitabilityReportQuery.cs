using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Reports;

public record ProfitabilityByTechnicianDto(
    Guid UserId,
    string TechnicianName,
    int TotalInterventions,
    int CompletedInterventions,
    decimal MaterialsCost,
    decimal QuotedRevenue
);

public record ProfitabilityByClientDto(
    Guid ClientId,
    string ClientName,
    int TotalInterventions,
    int CompletedInterventions,
    decimal MaterialsCost,
    decimal QuotedRevenue
);

public record ProfitabilityReportDto(
    IReadOnlyList<ProfitabilityByTechnicianDto> ByTechnician,
    IReadOnlyList<ProfitabilityByClientDto> ByClient,
    decimal TotalMaterialsCost,
    decimal TotalQuotedRevenue,
    DateTime From,
    DateTime To
);

public record GetProfitabilityReportQuery(DateTime? From, DateTime? To) : IRequest<Result<ProfitabilityReportDto>>;

public class GetProfitabilityReportQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IPlanGateService planGate)
    : IRequestHandler<GetProfitabilityReportQuery, Result<ProfitabilityReportDto>>
{
    public async Task<Result<ProfitabilityReportDto>> Handle(
        GetProfitabilityReportQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var ownerId = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.OwnerId ?? u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var ownerUser = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == ownerId, cancellationToken);

        if (ownerUser is null) return Result.Unauthorized();

        if (!planGate.CanUseAdvancedReports(ownerUser.Plan))
            return Result.Error("Os relatórios de rentabilidade requerem o plano Enterprise.");

        var from = request.From ?? DateTime.UtcNow.AddMonths(-3);
        var to = request.To?.AddDays(1) ?? DateTime.UtcNow;

        // Load interventions in the date range for this owner
        var interventions = await db.Interventions
            .AsNoTracking()
            .Include(i => i.Client)
            .Include(i => i.Quote)
                .ThenInclude(q => q!.Lines)
            .Where(i => i.UserId == ownerId &&
                        i.CreatedAt >= from && i.CreatedAt < to)
            .ToListAsync(cancellationToken);

        // Load assigned technician names
        var assignedUserIds = interventions
            .Where(i => i.AssignedToUserId.HasValue)
            .Select(i => i.AssignedToUserId!.Value)
            .Distinct()
            .ToList();

        var techNames = await db.Users.AsNoTracking()
            .Where(u => assignedUserIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName })
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        // By technician
        var byTech = interventions
            .GroupBy(i => i.AssignedToUserId ?? ownerId)
            .Select(g =>
            {
                var techId = g.Key;
                var name = techId == ownerId
                    ? (ownerUser.FullName + " (proprietário)")
                    : techNames.GetValueOrDefault(techId, "Desconhecido");
                var matCost = g.Sum(i => i.Materials.Sum(m => m.Quantity * m.UnitCost));
                var revenue = g.Sum(i => i.Quote?.Lines.Sum(l => l.Quantity * l.UnitPrice) ?? 0m);
                return new ProfitabilityByTechnicianDto(
                    techId, name,
                    g.Count(),
                    g.Count(i => i.Status == InterventionStatus.Completed),
                    matCost, revenue);
            })
            .OrderByDescending(x => x.TotalInterventions)
            .ToList();

        // By client
        var byClient = interventions
            .GroupBy(i => i.ClientId)
            .Select(g =>
            {
                var matCost = g.Sum(i => i.Materials.Sum(m => m.Quantity * m.UnitCost));
                var revenue = g.Sum(i => i.Quote?.Lines.Sum(l => l.Quantity * l.UnitPrice) ?? 0m);
                return new ProfitabilityByClientDto(
                    g.Key,
                    g.First().Client.Name,
                    g.Count(),
                    g.Count(i => i.Status == InterventionStatus.Completed),
                    matCost, revenue);
            })
            .OrderByDescending(x => x.QuotedRevenue)
            .ToList();

        return Result.Success(new ProfitabilityReportDto(
            byTech, byClient,
            byTech.Sum(t => t.MaterialsCost),
            byTech.Sum(t => t.QuotedRevenue),
            from, to));
    }
}
