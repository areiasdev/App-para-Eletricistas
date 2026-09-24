using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecnicoApp.Application.Common.Email;
using TecnicoApp.Application.Common.Formatting;
using TecnicoApp.Application.Common.Documents;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Domain.Enums;
using TecnicoApp.Application.Features.Quotes.DTOs;
using TecnicoApp.Application.Common.Extensions;

namespace TecnicoApp.Application.Features.Quotes.Commands.SendQuoteEmail;

public class SendQuoteEmailCommandHandler(
    IAppDbContext db,
    ICurrentUserService currentUser,
    IPdfService pdfService,
    IEmailService emailService,
    IFileStorageService fileStorage,
    INotificationService notificationService,
    IQuoteApprovalLinkService approvalLinks,
    IAppSettings appSettings,
    ILogger<SendQuoteEmailCommandHandler> logger)
    : IRequestHandler<SendQuoteEmailCommand, Result>
{
    public async Task<Result> Handle(SendQuoteEmailCommand request, CancellationToken cancellationToken)
    {
        // Resolve ownerId: team members share their owner's quotes
        var ownerId = await db.ResolveOwnerIdAsync(currentUser.UserId, cancellationToken);

        var quote = await db.Quotes
            .Include(q => q.Lines)
            .Include(q => q.Client)
            .FirstOrDefaultAsync(q => q.Id == request.QuoteId, cancellationToken);

        if (quote is null) return Result.NotFound();
        if (quote.UserId != ownerId) return Result.Forbidden();

        if (string.IsNullOrWhiteSpace(quote.Client?.Email))
            return Result.Invalid(new ValidationError(
                "ClientEmail",
                "O cliente não tem email registado. Adiciona um email ao cliente para poder enviar o orçamento."));

        // Issuer details always come from the team owner, not whoever sent the email —
        // a technician's own profile is usually blank and isn't the company's identity.
        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == ownerId, cancellationToken);

        if (user is null) return Result.Unauthorized();

        // Generate PDF
        var logoBytes = await fileStorage.ReadUploadBytesAsync(user.LogoUrl, cancellationToken);

        var pdfData = PdfDataFactory.ForQuote(quote, user, logoBytes);

        byte[] pdfBytes;
        try { pdfBytes = pdfService.GenerateQuotePdf(pdfData); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate PDF for quote {QuoteId}", request.QuoteId);
            return Result.Error("Não foi possível gerar o PDF do orçamento.");
        }

        var branding = EmailBranding.ForCompany(user);

        // Link where the client reviews the quote and accepts (with signature) or rejects it
        // online — offered while the quote is still open for a decision.
        string? approvalUrl = null;
        if (quote.Status is QuoteStatus.Draft or QuoteStatus.Sent)
            approvalUrl = $"{appSettings.BaseUrl}/orcamento/{approvalLinks.GetOrCreateToken(quote)}";
        var totalFormatted = PtFormat.Currency(quote.Total);
        var body = $"""
            <p style="margin:0 0 8px;color:#6b7280;font-size:13px;font-weight:600;text-transform:uppercase;letter-spacing:.05em;">Orçamento</p>
            <p style="margin:0 0 24px;color:#1a1a1a;font-size:28px;font-weight:700;font-family:monospace;">{EmailLayout.Encode(quote.Number)}</p>
            <p style="margin:0 0 16px;">Olá {EmailLayout.Encode(quote.Client.Name)},</p>
            <p style="margin:0 0 24px;"><strong>{EmailLayout.Encode(branding.SenderName)}</strong> enviou-te um orçamento. Encontras o PDF em anexo com todos os detalhes.</p>
            {EmailLayout.SummaryTable(
                ("Total", totalFormatted),
                ("Válido até", quote.ValidUntil.HasValue ? PtFormat.LongDate(quote.ValidUntil.Value) : "—"))}
            {(approvalUrl is null ? "" : EmailLayout.Button(branding, approvalUrl, "Ver e aceitar orçamento →"))}
            <p style="margin:0;color:#6b7280;font-size:13px;">Para qualquer questão, contacta diretamente <strong>{EmailLayout.Encode(branding.SenderName)}</strong>.</p>
            """;

        var attachment = new EmailAttachment(
            FileName: $"orcamento-{quote.Number}.pdf",
            Content: pdfBytes,
            ContentType: "application/pdf");

        await emailService.SendAsync(new EmailMessage(
            To: quote.Client.Email,
            ToName: quote.Client.Name,
            Subject: EmailLayout.SubjectSafe($"Orçamento {quote.Number} de {branding.SenderName}"),
            HtmlBody: EmailLayout.Render(branding, body, $"Enviado por {branding.SenderName}"),
            Attachments: [attachment]
        ), cancellationToken);

        // Persisted so the "sent" state survives a page reload — previously this was
        // tracked only in frontend component state and reset on every remount.
        quote.EmailSentAt = DateTime.UtcNow;
        // Emailing a draft is sending it — done here (not as a second call from the UI) so the
        // status can't be left behind if that follow-up request fails.
        if (quote.Status == QuoteStatus.Draft)
            quote.Status = QuoteStatus.Sent;
        await db.SaveChangesAsync(cancellationToken);

        // Opt-in WhatsApp ping. The email already succeeded and is the primary channel — this
        // is a bonus notification, so a failure here must never fail the overall command.
        if (quote.Client.WhatsAppOptIn && quote.Client.PhoneVerified &&
            !string.IsNullOrWhiteSpace(quote.Client.Phone))
        {
            try
            {
                var waMessage =
                    $"Olá {quote.Client.Name}, {branding.SenderName} enviou-te um orçamento ({quote.Number}) " +
                    $"no valor de {totalFormatted}. " +
                    (approvalUrl is null ? "Consulta o teu email para o PDF." : $"Vê e aceita aqui: {approvalUrl}");

                await notificationService.SendWhatsAppAsync(quote.Client.Phone, waMessage, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to send WhatsApp notification for quote {QuoteId}", request.QuoteId);
            }
        }

        return Result.Success();
    }
}
