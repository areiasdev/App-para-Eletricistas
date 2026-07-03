using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.DTOs;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.AuditLogs.DTOs;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.AuditLogs.Queries.GetAuditLogs;

public class GetAuditLogsQueryHandler(
    IAppDbContext db,
    ICurrentUserService currentUser,
    IPlanGateService planGate)
    : IRequestHandler<GetAuditLogsQuery, Result<PaginatedResult<AuditLogDto>>>
{
    public async Task<Result<PaginatedResult<AuditLogDto>>> Handle(
        GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var callingUser = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (callingUser is null) return Result.Unauthorized();

        var ownerId = callingUser.OwnerId ?? callingUser.Id;

        var ownerPlan = callingUser.OwnerId.HasValue
            ? await db.Users.AsNoTracking()
                .Where(u => u.Id == ownerId)
                .Select(u => u.Plan)
                .FirstOrDefaultAsync(cancellationToken)
            : callingUser.Plan;

        if (!planGate.CanUseAdvancedReports(ownerPlan))
            return Result.Forbidden();

        // Collect all user IDs belonging to this tenant
        var teamUserIds = await db.TeamMembers.AsNoTracking()
            .Where(t => t.OwnerId == ownerId)
            .Select(t => t.MemberId)
            .ToListAsync(cancellationToken);
        teamUserIds.Add(ownerId);

        var query = db.AuditLogs.AsNoTracking()
            .Where(a => a.UserId == null || teamUserIds.Contains(a.UserId.Value));

        if (!string.IsNullOrWhiteSpace(request.EntityType))
            query = query.Where(a => a.EntityType == request.EntityType);

        if (request.From.HasValue)
            query = query.Where(a => a.OccurredAt >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(a => a.OccurredAt <= request.To.Value.AddDays(1));

        var total = await query.CountAsync(cancellationToken);

        var page     = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(a => a.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto(
                a.Id, a.EntityType, a.EntityId, a.Action,
                a.UserId, a.UserEmail, a.Changes, a.OccurredAt))
            .ToListAsync(cancellationToken);

        return Result.Success(new PaginatedResult<AuditLogDto>(items, total, page, pageSize));
    }
}
