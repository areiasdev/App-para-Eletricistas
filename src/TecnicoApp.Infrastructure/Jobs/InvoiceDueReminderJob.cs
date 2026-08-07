using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Domain.Enums;
using TecnicoApp.Infrastructure.Persistence;

namespace TecnicoApp.Infrastructure.Jobs;

public class InvoiceDueReminderJob(
    AppDbContext db,
    INotificationService notificationService,
    ILogger<InvoiceDueReminderJob> logger)
{
    /// <summary>
    /// Runs daily. Sends one WhatsApp reminder per invoice whose DueDate is exactly 3 days away.
    /// Uses a ±1 day window around "today + 3 days" to handle timing drift, mirroring
    /// MaintenanceAlertJob's windowing approach.
    /// </summary>
    /// <remarks>
    /// AutomaticRetry is disabled: a mid-run failure after some reminders already sent would
    /// otherwise cause Hangfire to replay the whole batch and double-send to clients already
    /// notified. Missing a run is cheap — it retries naturally on the next daily schedule.
    /// </remarks>
    [AutomaticRetry(Attempts = 0)]
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var targetDate = DateTime.UtcNow.Date.AddDays(3);
        var windowStart = targetDate.AddDays(-1);
        var windowEnd = targetDate.AddDays(1);

        var invoices = await db.Invoices
            .AsNoTracking()
            .Include(i => i.Client)
            .Where(i =>
                !i.IsDeleted &&
                (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.Overdue) &&
                i.DueDate >= windowStart &&
                i.DueDate < windowEnd)
            .ToListAsync(cancellationToken);

        logger.LogInformation(
            "[InvoiceDueReminder] Found {Count} invoices due around {Date}",
            invoices.Count, targetDate.ToString("yyyy-MM-dd"));

        var sent = 0;
        var failed = 0;

        foreach (var invoice in invoices)
        {
            var client = invoice.Client;
            if (client is null) continue;
            if (!client.WhatsAppOptIn || !client.PhoneVerified) continue;
            if (string.IsNullOrWhiteSpace(client.Phone)) continue;

            try
            {
                var dueStr = invoice.DueDate.ToString("dd/MM/yyyy");
                var totalFormatted = invoice.Total.ToString("C", new System.Globalization.CultureInfo("pt-PT"));
                var message =
                    $"Olá {client.Name}, a tua fatura {invoice.Number} no valor de {totalFormatted} " +
                    $"vence a {dueStr}. Consulta o teu email para efetuar o pagamento.";

                await notificationService.SendWhatsAppAsync(client.Phone, message, cancellationToken);

                logger.LogInformation(
                    "[InvoiceDueReminder] Reminder sent to {Phone} for invoice {InvoiceId}",
                    client.Phone, invoice.Id);
                sent++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "[InvoiceDueReminder] Failed to send reminder to {Phone} for invoice {InvoiceId}",
                    client.Phone, invoice.Id);
                failed++;
            }
        }

        logger.LogInformation(
            "[InvoiceDueReminder] Completed. Sent: {Sent}, Failed: {Failed}", sent, failed);
    }
}
