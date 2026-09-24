using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecnicoApp.Domain.Enums;
using TecnicoApp.Infrastructure.Persistence;

namespace TecnicoApp.Infrastructure.Jobs;

public class InvoiceOverdueJob(AppDbContext db, ILogger<InvoiceOverdueJob> logger)
{
    /// <summary>
    /// Runs daily. Moves every Issued invoice whose DueDate has passed to Overdue, so the
    /// invoice list, dashboard and filters reflect reality without someone flipping each one by
    /// hand. Idempotent (only touches Issued rows), so Hangfire's default retries are safe.
    /// Entities are loaded and saved (not ExecuteUpdate) so each change lands in the audit log.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        var overdue = await db.Invoices
            .Where(i => i.Status == InvoiceStatus.Issued && i.DueDate < today)
            .ToListAsync(cancellationToken);

        foreach (var invoice in overdue)
        {
            invoice.Status = InvoiceStatus.Overdue;
            invoice.ModifiedBy = "sistema";
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("[InvoiceOverdue] Marked {Count} invoices as overdue", overdue.Count);
    }
}
