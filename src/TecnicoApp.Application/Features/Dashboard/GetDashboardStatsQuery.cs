using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Dashboard;

public record UpcomingMaintenanceDto(
    Guid EquipmentId,
    string Type,
    string? Brand,
    string? Model,
    string ClientName,
    DateTime NextMaintenance,
    int DaysUntil
);

public record DashboardStatsDto(
    int TotalClients,
    int TotalQuotes,
    int DraftQuotes,
    int SentQuotes,
    int AcceptedQuotes,
    decimal TotalRevenue,        // Invoiced quotes
    decimal PendingRevenue,      // Accepted quotes not yet invoiced
    int TotalInterventions,
    int ScheduledInterventions,
    int InProgressInterventions,
    IReadOnlyList<RecentQuoteDto> RecentQuotes,
    IReadOnlyList<UpcomingMaintenanceDto> UpcomingMaintenance
);

public record RecentQuoteDto(
    Guid Id,
    string Number,
    QuoteStatus Status,
    string ClientName,
    decimal Total,
    DateTime CreatedAt
);

public record GetDashboardStatsQuery : IRequest<Result<DashboardStatsDto>>;

public class GetDashboardStatsQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetDashboardStatsQuery, Result<DashboardStatsDto>>
{
    public async Task<Result<DashboardStatsDto>> Handle(
        GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        // Resolve ownerId: team members see their owner's team-wide stats
        var ownerId = await db.Users.AsNoTracking()
            .Where(u => u.Id == currentUser.UserId)
            .Select(u => u.OwnerId ?? u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        // All counts and aggregates done in SQL — no in-memory loading
        var clientsCount = await db.Clients
            .CountAsync(c => c.UserId == ownerId, cancellationToken);

        var quoteCounts = await db.Quotes
            .AsNoTracking()
            .Where(q => q.UserId == ownerId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Draft = g.Count(q => q.Status == QuoteStatus.Draft),
                Sent = g.Count(q => q.Status == QuoteStatus.Sent),
                Accepted = g.Count(q => q.Status == QuoteStatus.Accepted),
                TotalRevenue = g
                    .Where(q => q.Status == QuoteStatus.Invoiced)
                    .Sum(q => (decimal?)q.Lines.Sum(l => l.Quantity * l.UnitPrice * (1 + l.VatRate / 100m)) - (q.Discount ?? 0)) ?? 0m,
                PendingRevenue = g
                    .Where(q => q.Status == QuoteStatus.Accepted)
                    .Sum(q => (decimal?)q.Lines.Sum(l => l.Quantity * l.UnitPrice * (1 + l.VatRate / 100m)) - (q.Discount ?? 0)) ?? 0m,
            })
            .FirstOrDefaultAsync(cancellationToken);

        var recentQuotes = await db.Quotes
            .AsNoTracking()
            .Where(q => q.UserId == ownerId)
            .OrderByDescending(q => q.CreatedAt)
            .Take(5)
            .Select(q => new RecentQuoteDto(
                q.Id,
                q.Number,
                q.Status,
                q.Client.Name,
                q.Lines.Sum(l => l.Quantity * l.UnitPrice * (1 + l.VatRate / 100m)) - (q.Discount ?? 0),
                q.CreatedAt))
            .ToListAsync(cancellationToken);

        var interventionCounts = await db.Interventions
            .AsNoTracking()
            .Where(i => i.UserId == ownerId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Scheduled = g.Count(i => i.Status == InterventionStatus.Scheduled),
                InProgress = g.Count(i => i.Status == InterventionStatus.InProgress),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var today = DateTime.UtcNow.Date;
        var limit = today.AddDays(30);

        var upcomingMaintenance = await db.Equipment
            .AsNoTracking()
            .Where(e =>
                e.Client.UserId == ownerId &&
                e.NextMaintenance.HasValue &&
                e.NextMaintenance.Value >= today &&
                e.NextMaintenance.Value <= limit)
            .OrderBy(e => e.NextMaintenance)
            .Take(5)
            .Select(e => new UpcomingMaintenanceDto(
                e.Id,
                e.Type,
                e.Brand,
                e.Model,
                e.Client.Name,
                e.NextMaintenance!.Value,
                (int)(e.NextMaintenance!.Value.Date - today).TotalDays))
            .ToListAsync(cancellationToken);

        var stats = new DashboardStatsDto(
            clientsCount,
            quoteCounts?.Total ?? 0,
            quoteCounts?.Draft ?? 0,
            quoteCounts?.Sent ?? 0,
            quoteCounts?.Accepted ?? 0,
            quoteCounts?.TotalRevenue ?? 0m,
            quoteCounts?.PendingRevenue ?? 0m,
            interventionCounts?.Total ?? 0,
            interventionCounts?.Scheduled ?? 0,
            interventionCounts?.InProgress ?? 0,
            recentQuotes,
            upcomingMaintenance
        );

        return Result.Success(stats);
    }
}
