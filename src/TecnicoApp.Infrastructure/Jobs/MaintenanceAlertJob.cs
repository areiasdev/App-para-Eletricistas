using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecnicoApp.Application.Common.Email;
using TecnicoApp.Application.Common.Formatting;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Infrastructure.Persistence;

namespace TecnicoApp.Infrastructure.Jobs;

public class MaintenanceAlertJob(
    AppDbContext db,
    IEmailService emailService,
    IAppSettings appSettings,
    ILogger<MaintenanceAlertJob> logger)
{
    /// <summary>How far ahead of NextMaintenance the owner is alerted.</summary>
    public const int DaysAhead = 7;

    /// <summary>
    /// Runs daily. Sends one email per equipment whose NextMaintenance is exactly <see cref="DaysAhead"/> days away.
    /// Uses a window of ±12h around "today + 7 days" to handle timing drift.
    /// </summary>
    /// <remarks>
    /// AutomaticRetry is disabled: a mid-run failure after some alerts already sent would
    /// otherwise cause Hangfire to replay the whole batch and double-send to clients already
    /// notified. Missing a run is cheap — it retries naturally on the next daily schedule.
    /// </remarks>
    [AutomaticRetry(Attempts = 0)]
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var targetDate = DateTime.UtcNow.Date.AddDays(DaysAhead);
        var windowStart = targetDate;
        var windowEnd   = targetDate.AddDays(1);

        var equipment = await db.Equipment
            .AsNoTracking()
            .Include(e => e.Client)
                .ThenInclude(c => c.User)
            .Where(e =>
                !e.IsDeleted &&
                e.NextMaintenance.HasValue &&
                e.NextMaintenance.Value >= windowStart &&
                e.NextMaintenance.Value < windowEnd)
            .ToListAsync(cancellationToken);

        logger.LogInformation(
            "[MaintenanceAlert] Found {Count} equipment items due for maintenance on {Date}",
            equipment.Count, targetDate.ToString("yyyy-MM-dd"));

        var sent = 0;
        var failed = 0;

        foreach (var eq in equipment)
        {
            var user = eq.Client.User;
            if (user is null) continue;

            try
            {
                var subject = EmailLayout.SubjectSafe($"🔧 Manutenção agendada em {DaysAhead} dias — {eq.Client.Name} · {eq.Type}");
                var html = BuildEmailHtml(eq.Type, eq.Brand, eq.Model, eq.Client.Name,
                    eq.NextMaintenance!.Value, user.FullName, appSettings.BaseUrl, eq.Id, appSettings.ProductName);

                await emailService.SendAsync(
                    new EmailMessage(user.Email, user.FullName, subject, html), cancellationToken);

                logger.LogInformation(
                    "[MaintenanceAlert] Alert sent to {Email} for equipment {EquipmentId}",
                    user.Email, eq.Id);
                sent++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "[MaintenanceAlert] Failed to send alert to {Email} for equipment {EquipmentId}",
                    user.Email, eq.Id);
                failed++;
            }
        }

        logger.LogInformation(
            "[MaintenanceAlert] Completed. Sent: {Sent}, Failed: {Failed}", sent, failed);
    }

    private static string BuildEmailHtml(
        string type, string? brand, string? model,
        string clientName, DateTime nextMaintenance, string userName,
        string baseUrl, Guid equipmentId, string productName)
    {
        var equipmentLabel = string.Join(" ", new[] { type, brand, model }.Where(s => !string.IsNullOrWhiteSpace(s)));
        var branding = new EmailBranding(productName);

        var body = $"""
            <p style="margin:0 0 8px;color:#6b7280;">Olá, <strong style="color:#1a1a1a">{EmailLayout.Encode(userName)}</strong></p>
            <h2 style="margin:0 0 24px;color:#1a1a1a;font-size:18px">Manutenção agendada para daqui a {DaysAhead} dias</h2>
            {EmailLayout.SummaryTable(
                ("Equipamento", equipmentLabel),
                ("Cliente", clientName),
                ("Data de manutenção", PtFormat.ShortDate(nextMaintenance)))}
            {EmailLayout.Button(branding, $"{baseUrl}/dashboard/equipamentos/{equipmentId}", "Ver equipamento")}
            """;

        return EmailLayout.Render(branding, body, $"{productName} · Gestão de manutenções");
    }
}
