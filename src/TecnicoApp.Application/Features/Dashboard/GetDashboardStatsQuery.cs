using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Dashboard;

public record DashboardStatsDto(
    int TotalClients,
    int TotalQuotes,
    int DraftQuotes,
    int SentQuotes,
    int AcceptedQuotes,
    decimal TotalRevenue,        // Invoiced quotes
    decimal PendingRevenue,      // Accepted quotes not yet invoiced
    IReadOnlyList<RecentQuoteDto> RecentQuotes
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
        var userId = currentUser.UserId;

        var clientsCount = await db.Clients
            .CountAsync(c => c.UserId == userId, cancellationToken);

        var quotes = await db.Quotes
            .AsNoTracking()
            .Where(q => q.UserId == userId)
            .Include(q => q.Lines)
            .Include(q => q.Client)
            .ToListAsync(cancellationToken);

        var totalRevenue = quotes
            .Where(q => q.Status == QuoteStatus.Invoiced)
            .Sum(q => q.Total);

        var pendingRevenue = quotes
            .Where(q => q.Status == QuoteStatus.Accepted)
            .Sum(q => q.Total);

        var recentQuotes = quotes
            .OrderByDescending(q => q.CreatedAt)
            .Take(5)
            .Select(q => new RecentQuoteDto(
                q.Id, q.Number, q.Status, q.Client.Name, q.Total, q.CreatedAt))
            .ToList();

        var stats = new DashboardStatsDto(
            clientsCount,
            quotes.Count,
            quotes.Count(q => q.Status == QuoteStatus.Draft),
            quotes.Count(q => q.Status == QuoteStatus.Sent),
            quotes.Count(q => q.Status == QuoteStatus.Accepted),
            totalRevenue,
            pendingRevenue,
            recentQuotes
        );

        return Result.Success(stats);
    }
}
