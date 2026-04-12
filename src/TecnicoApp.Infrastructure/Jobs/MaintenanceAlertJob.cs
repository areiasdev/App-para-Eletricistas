using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Infrastructure.Persistence;

namespace TecnicoApp.Infrastructure.Jobs;

public class MaintenanceAlertJob(
    AppDbContext db,
    IEmailService emailService,
    ILogger<MaintenanceAlertJob> logger)
{
    /// <summary>
    /// Runs daily. Sends one email per equipment whose NextMaintenance is exactly 7 days away.
    /// Uses a window of ±12h around "today + 7 days" to handle timing drift.
    /// </summary>
    public async Task RunAsync()
    {
        var targetDate = DateTime.UtcNow.Date.AddDays(7);
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
            .ToListAsync();

        logger.LogInformation(
            "[MaintenanceAlert] Found {Count} equipment items due for maintenance on {Date}",
            equipment.Count, targetDate.ToString("yyyy-MM-dd"));

        foreach (var eq in equipment)
        {
            var user = eq.Client.User;
            if (user is null) continue;

            var subject = $"🔧 Manutenção agendada em 7 dias — {eq.Type}";
            var html = BuildEmailHtml(eq.Type, eq.Brand, eq.Model, eq.Client.Name,
                eq.NextMaintenance!.Value, user.FullName);

            await emailService.SendAsync(
                new EmailMessage(user.Email, user.FullName, subject, html));

            logger.LogInformation(
                "[MaintenanceAlert] Alert sent to {Email} for equipment {EquipmentId}",
                user.Email, eq.Id);
        }
    }

    private static string BuildEmailHtml(
        string type, string? brand, string? model,
        string clientName, DateTime nextMaintenance, string userName)
    {
        var equipmentLabel = string.Join(" ", new[] { type, brand, model }.Where(s => !string.IsNullOrWhiteSpace(s)));
        var dateStr = nextMaintenance.ToString("dd/MM/yyyy");

        return $"""
            <!DOCTYPE html>
            <html>
            <body style="font-family:Arial,sans-serif;background:#f7f7f4;margin:0;padding:20px">
              <div style="max-width:560px;margin:0 auto;background:white;border-radius:12px;overflow:hidden">
                <div style="background:#f59e0b;padding:24px 32px">
                  <h1 style="margin:0;color:white;font-size:20px">⚡ TécnicoApp</h1>
                </div>
                <div style="padding:32px">
                  <p style="color:#6b7280;margin:0 0 8px">Olá, <strong style="color:#1a1a1a">{userName}</strong></p>
                  <h2 style="margin:0 0 24px;color:#1a1a1a;font-size:18px">
                    Manutenção agendada para daqui a 7 dias
                  </h2>
                  <div style="background:#fffbeb;border:1px solid #fcd34d;border-radius:8px;padding:20px;margin-bottom:24px">
                    <p style="margin:0 0 4px;font-size:14px;color:#92400e"><strong>Equipamento</strong></p>
                    <p style="margin:0 0 12px;font-size:16px;color:#1a1a1a">{equipmentLabel}</p>
                    <p style="margin:0 0 4px;font-size:14px;color:#92400e"><strong>Cliente</strong></p>
                    <p style="margin:0 0 12px;font-size:16px;color:#1a1a1a">{clientName}</p>
                    <p style="margin:0 0 4px;font-size:14px;color:#92400e"><strong>Data de manutenção</strong></p>
                    <p style="margin:0;font-size:20px;font-weight:bold;color:#b45309">{dateStr}</p>
                  </div>
                  <p style="color:#6b7280;font-size:14px">
                    Acede ao TécnicoApp para ver os detalhes do equipamento e agendar a intervenção.
                  </p>
                </div>
                <div style="padding:16px 32px;background:#f7f7f4;text-align:center">
                  <p style="margin:0;font-size:12px;color:#9ca3af">
                    Enviado pelo TécnicoApp · Gestão de manutenções
                  </p>
                </div>
              </div>
            </body>
            </html>
            """;
    }
}
