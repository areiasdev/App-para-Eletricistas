using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Domain.Enums;
using TecnicoApp.Infrastructure.Persistence;

namespace TecnicoApp.Infrastructure.Jobs;

public class AppointmentReminderJob(
    AppDbContext db,
    INotificationService notificationService,
    ILogger<AppointmentReminderJob> logger)
{
    /// <summary>
    /// Runs daily. Sends one WhatsApp reminder to the *client* per scheduled intervention whose
    /// ScheduledAt is tomorrow (the day before the appointment) — distinct from
    /// MaintenanceAlertJob, which reminds the technician about upcoming equipment maintenance.
    /// Uses a ±1 day window around "tomorrow" to handle timing drift, mirroring
    /// MaintenanceAlertJob's windowing approach.
    /// </summary>
    public async Task RunAsync()
    {
        var targetDate = DateTime.UtcNow.Date.AddDays(1);
        var windowStart = targetDate.AddDays(-1);
        var windowEnd = targetDate.AddDays(1);

        var interventions = await db.Interventions
            .AsNoTracking()
            .Include(i => i.Client)
            .Where(i =>
                !i.IsDeleted &&
                i.Status == InterventionStatus.Scheduled &&
                i.ScheduledAt.HasValue &&
                i.ScheduledAt.Value >= windowStart &&
                i.ScheduledAt.Value < windowEnd)
            .ToListAsync();

        logger.LogInformation(
            "[AppointmentReminder] Found {Count} interventions scheduled around {Date}",
            interventions.Count, targetDate.ToString("yyyy-MM-dd"));

        var sent = 0;
        var failed = 0;

        foreach (var intervention in interventions)
        {
            var client = intervention.Client;
            if (client is null) continue;
            if (!client.WhatsAppOptIn || !client.PhoneVerified) continue;
            if (string.IsNullOrWhiteSpace(client.Phone)) continue;

            try
            {
                var scheduledStr = intervention.ScheduledAt!.Value.ToString("dd/MM/yyyy 'às' HH:mm");
                var message =
                    $"Olá {client.Name}, lembramos que tens uma intervenção agendada " +
                    $"({intervention.Title}) para amanhã, dia {scheduledStr}.";

                await notificationService.SendWhatsAppAsync(client.Phone, message);

                logger.LogInformation(
                    "[AppointmentReminder] Reminder sent to {Phone} for intervention {InterventionId}",
                    client.Phone, intervention.Id);
                sent++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "[AppointmentReminder] Failed to send reminder to {Phone} for intervention {InterventionId}",
                    client.Phone, intervention.Id);
                failed++;
            }
        }

        logger.LogInformation(
            "[AppointmentReminder] Completed. Sent: {Sent}, Failed: {Failed}", sent, failed);
    }
}
