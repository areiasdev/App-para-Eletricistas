using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecnicoApp.Application.Common.Email;
using TecnicoApp.Application.Common.Formatting;
using TecnicoApp.Application.Common.Interfaces;
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
        var lineDtos = quote.Lines.ToLineDtos();

        var logoBytes = await fileStorage.ReadLogoBytesAsync(user.LogoUrl, cancellationToken);

        var pdfData = new QuotePdfData(
            Number: quote.Number,
            CreatedAt: quote.CreatedAt,
            ValidUntil: quote.ValidUntil,
            Notes: quote.Notes,
            ClientName: quote.Client.Name,
            ClientEmail: quote.Client.Email,
            ClientPhone: quote.Client.Phone,
            ClientNif: quote.Client.Nif,
            IssuerName: user.FullName,
            IssuerCompany: user.CompanyName,
            IssuerEmail: user.Email,
            IssuerPhone: user.Phone,
            IssuerNif: user.Nif,
            IssuerLogoBytes: logoBytes,
            IssuerBrandColorHex: user.BrandColor,
            Lines: lineDtos,
            SubTotal: quote.SubTotal,
            VatTotal: quote.VatTotal,
            Discount: quote.Discount,
            Total: quote.Total
        );

        byte[] pdfBytes;
        try { pdfBytes = pdfService.GenerateQuotePdf(pdfData); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate PDF for quote {QuoteId}", request.QuoteId);
            return Result.Error("Não foi possível gerar o PDF do orçamento.");
        }

        var branding = EmailBranding.ForCompany(user);
        var totalFormatted = PtFormat.Currency(quote.Total);
        var body = $"""
            <p style="margin:0 0 8px;color:#6b7280;font-size:13px;font-weight:600;text-transform:uppercase;letter-spacing:.05em;">Orçamento</p>
            <p style="margin:0 0 24px;color:#1a1a1a;font-size:28px;font-weight:700;font-family:monospace;">{EmailLayout.Encode(quote.Number)}</p>
            <p style="margin:0 0 16px;">Olá {EmailLayout.Encode(quote.Client.Name)},</p>
            <p style="margin:0 0 24px;"><strong>{EmailLayout.Encode(branding.SenderName)}</strong> enviou-te um orçamento. Encontras o PDF em anexo com todos os detalhes.</p>
            {EmailLayout.SummaryTable(
                ("Total", totalFormatted),
                ("Válido até", quote.ValidUntil.HasValue ? PtFormat.LongDate(quote.ValidUntil.Value) : "—"))}
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
                    $"no valor de {totalFormatted}. Consulta o teu email para o PDF.";

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
