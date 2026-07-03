using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecnicoApp.Domain.Enums;
using TecnicoApp.Infrastructure.Persistence;

namespace TecnicoApp.Infrastructure.Jobs;

public class TrialExpirationJob(AppDbContext db, ILogger<TrialExpirationJob> logger)
{
    public async Task RunAsync()
    {
        var expired = await db.Users
            .Where(u =>
                u.TrialEndsAt.HasValue &&
                u.TrialEndsAt.Value < DateTime.UtcNow &&
                u.Plan == Plan.Enterprise &&
                u.StripeCustomerId == null &&
                u.OwnerId == null) // only owners, not team members
            .ToListAsync();

        if (expired.Count == 0) return;

        foreach (var user in expired)
        {
            user.Plan = Plan.Free;
            logger.LogInformation("[Trial] Expired for {Email} — downgraded to Free", user.Email);
        }

        await db.SaveChangesAsync();
        logger.LogInformation("[Trial] Downgraded {Count} expired trial accounts", expired.Count);
    }
}
