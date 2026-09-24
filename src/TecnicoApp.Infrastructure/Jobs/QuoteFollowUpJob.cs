using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecnicoApp.Application.Common.Email;
using TecnicoApp.Application.Common.Formatting;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Domain.Enums;
using TecnicoApp.Infrastructure.Persistence;

namespace TecnicoApp.Infrastructure.Jobs;

public class QuoteFollowUpJob(
    AppDbContext db,
    IEmailService emailService,
    IAppSettings appSettings,
    ILogger<QuoteFollowUpJob> logger)
{
    /// <summary>A sent quote with no answer after this many days gets a follow-up nudge.</summary>
    public const int DaysWithoutAnswer = 7;

    /// <summary>
    /// Runs daily. Sends each company owner one digest of quotes that were emailed
    /// <see cref="DaysWithoutAnswer"/>+ days ago and are still waiting — unanswered quotes are
    /// the most common way small trades lose work. Each quote is included once
    /// (Quote.FollowUpSentAt), so a failed/replayed run never nags twice about the same quote.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-DaysWithoutAnswer);

        var pending = await db.Quotes
            .Include(q => q.Client)
            .Include(q => q.User)
            .Include(q => q.Lines)
            .Where(q => q.Status == QuoteStatus.Sent
                        && q.EmailSentAt != null && q.EmailSentAt <= cutoff
                        && q.FollowUpSentAt == null)
            .ToListAsync(cancellationToken);

        foreach (var ownerQuotes in pending.GroupBy(q => q.UserId))
        {
            var owner = ownerQuotes.First().User;
            var branding = new EmailBranding(appSettings.ProductName);
            var rows = ownerQuotes
                .OrderBy(q => q.EmailSentAt)
                .Select(q => (
                    $"{q.Number} · {q.Client.Name}",
                    $"{PtFormat.Currency(q.Total)} · enviado {PtFormat.ShortDate(q.EmailSentAt!.Value)}"))
                .ToArray();

            var body = $"""
                <h2 style="margin:0 0 8px;color:#1a1a1a;font-size:18px;">{rows.Length} orçamento(s) sem resposta</h2>
                <p style="margin:0 0 20px;">Enviados há mais de {DaysWithoutAnswer} dias e ainda sem resposta do cliente. Um telefonema rápido costuma fechar o negócio.</p>
                {EmailLayout.SummaryTable(rows)}
                {EmailLayout.Button(branding, $"{appSettings.BaseUrl}/dashboard/orcamentos?status=Sent", "Ver orçamentos enviados")}
                """;

            try
            {
                await emailService.SendAsync(new EmailMessage(
                    owner.Email, owner.FullName,
                    $"📋 {rows.Length} orçamento(s) à espera de resposta",
                    EmailLayout.Render(branding, body, appSettings.ProductName)), cancellationToken);

                foreach (var quote in ownerQuotes)
                    quote.FollowUpSentAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[QuoteFollowUp] Failed to send digest to {Email}", owner.Email);
            }
        }

        logger.LogInformation("[QuoteFollowUp] {Count} unanswered quotes reported", pending.Count);
    }
}
