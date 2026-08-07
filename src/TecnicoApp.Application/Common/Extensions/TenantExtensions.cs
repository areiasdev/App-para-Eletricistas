using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Common.Extensions;

/// <summary>Tenant id plus the caller's own role, for handlers that also gate on Owner/Admin.</summary>
public record CallerInfo(Guid OwnerId, UserRole Role);

public static class TenantExtensions
{
    /// <summary>
    /// Resolves the tenant id for a caller: team members share their owner's tenant
    /// (<see cref="TecnicoApp.Domain.Entities.User.OwnerId"/>), while an Owner's own Id is the
    /// tenant. Returns <see cref="Guid.Empty"/> if no user row matches <paramref name="callerId"/>
    /// (e.g. a deleted/unknown caller) — same behavior as the equivalent inline query it replaces.
    /// </summary>
    public static Task<Guid> ResolveOwnerIdAsync(
        this IAppDbContext db, Guid callerId, CancellationToken cancellationToken) =>
        db.Users.AsNoTracking()
            .Where(u => u.Id == callerId)
            .Select(u => u.OwnerId ?? u.Id)
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Like <see cref="ResolveOwnerIdAsync"/>, but also returns the caller's own Role for
    /// handlers that gate an action on it (e.g. delete/status-change requiring Owner/Admin).
    /// Returns null if no user row matches <paramref name="callerId"/>.
    /// </summary>
    public static Task<CallerInfo?> ResolveCallerAsync(
        this IAppDbContext db, Guid callerId, CancellationToken cancellationToken) =>
        db.Users.AsNoTracking()
            .Where(u => u.Id == callerId)
            .Select(u => new CallerInfo(u.OwnerId ?? u.Id, u.Role))
            .FirstOrDefaultAsync(cancellationToken);
}
