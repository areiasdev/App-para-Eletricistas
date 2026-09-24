using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecnicoApp.Application.Common.Email;
using TecnicoApp.Application.Common.Formatting;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Invoices.DTOs;
using TecnicoApp.Domain.Enums;
using TecnicoApp.Application.Common.Extensions;

namespace TecnicoApp.Application.Features.Invoices.Commands.SendInvoiceEmail;

public class SendInvoiceEmailCommandHandler(
    IAppDbContext db,
    ICurrentUserService currentUser,
    IPdfService pdfService,
    IEmailService emailService,
    IFileStorageService fileStorage,
    IInvoicePayLinkService payLinkService,
    IAppSettings appSettings,
    INotificationService notificationService,
    ILogger<SendInvoiceEmailCommandHandler> logger)
    : IRequestHandler<SendInvoiceEmailCommand, Result>
{
    public async Task<Result> Handle(SendInvoiceEmailCommand request, CancellationToken cancellationToken)
    {
        // Resolve ownerId: team members share their owner's invoices
        var caller = await db.ResolveCallerAsync(currentUser.UserId, cancellationToken);

        if (caller is null)
            return Result.Unauthorized();

        // Same gate as creating/managing invoices — sending one should be too.
        if (caller.Role is not (UserRole.Owner or UserRole.Admin))
            return Result.Forbidden("Apenas o proprietário ou administradores podem enviar faturas.");

        var ownerId = caller.OwnerId;

        var invoice = await db.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Client)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

        if (invoice is null) return Result.NotFound();
        if (invoice.UserId != ownerId) return Result.Forbidden();

        if (string.IsNullOrWhiteSpace(invoice.Client?.Email))
            return Result.Invalid(new ValidationError(
                "ClientEmail",
                "O cliente não tem email registado. Adiciona um email ao cliente para poder enviar a fatura."));

        // Issuer details always come from the team owner, not whoever sent the email.
        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == ownerId, cancellationToken);

        if (user is null) return Result.Unauthorized();

        var lineDtos = invoice.Lines.ToLineDtos();

        var logoBytes = await fileStorage.ReadLogoBytesAsync(user.LogoUrl, cancellationToken);

        var pdfData = new InvoicePdfData(
            Number: invoice.Number,
            IssuedAt: invoice.IssuedAt,
            DueDate: invoice.DueDate,
            Notes: invoice.Notes,
            ClientName: invoice.Client.Name,
            ClientEmail: invoice.Client.Email,
            ClientPhone: invoice.Client.Phone,
            ClientNif: invoice.Client.Nif,
            IssuerName: user.FullName,
            IssuerCompany: user.CompanyName,
            IssuerEmail: user.Email,
            IssuerPhone: user.Phone,
            IssuerNif: user.Nif,
            IssuerLogoBytes: logoBytes,
            IssuerBrandColorHex: user.BrandColor,
            IssuerIban: user.Iban,
            IssuerBankName: user.BankName,
            Lines: lineDtos,
            SubTotal: invoice.SubTotal,
            VatTotal: invoice.VatTotal,
            Discount: invoice.Discount,
            Total: invoice.Total
        );

        byte[] pdfBytes;
        try { pdfBytes = pdfService.GenerateInvoicePdf(pdfData); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate PDF for invoice {InvoiceId}", request.InvoiceId);
            return Result.Error("Não foi possível gerar o PDF da fatura.");
        }

        // Only offer a "Pagar agora" button while there's actually something left to pay —
        // reuses the same token-generation logic as GetOrCreateInvoicePayLinkCommand so a link
        // already sent in a previous email keeps working instead of being silently rotated.
        string? payUrl = null;
        var payable = invoice.Status is not (InvoiceStatus.Paid or InvoiceStatus.Cancelled);
        if (payable)
        {
            var rawToken = payLinkService.GetOrCreateToken(invoice);
            payUrl = $"{appSettings.BaseUrl}/pay/{rawToken}";
        }

        var branding = EmailBranding.ForCompany(user);
        var totalFormatted = PtFormat.Currency(invoice.Total);
        var payButtonHtml = payUrl is not null ? EmailLayout.Button(branding, payUrl, "Pagar agora →") : "";
        var body = $"""
            <p style="margin:0 0 8px;color:#6b7280;font-size:13px;font-weight:600;text-transform:uppercase;letter-spacing:.05em;">Fatura</p>
            <p style="margin:0 0 24px;color:#1a1a1a;font-size:28px;font-weight:700;font-family:monospace;">{EmailLayout.Encode(invoice.Number)}</p>
            <p style="margin:0 0 16px;">Olá {EmailLayout.Encode(invoice.Client.Name)},</p>
            <p style="margin:0 0 24px;"><strong>{EmailLayout.Encode(branding.SenderName)}</strong> enviou-te uma fatura. Encontras o PDF em anexo com todos os detalhes.</p>
            {EmailLayout.SummaryTable(("Total", totalFormatted), ("Vencimento", PtFormat.LongDate(invoice.DueDate)))}
            {payButtonHtml}
            <p style="margin:0;color:#6b7280;font-size:13px;">Para qualquer questão, contacta diretamente <strong>{EmailLayout.Encode(branding.SenderName)}</strong>.</p>
            """;

        var attachment = new EmailAttachment(
            FileName: $"fatura-{invoice.Number}.pdf",
            Content: pdfBytes,
            ContentType: "application/pdf");

        await emailService.SendAsync(new EmailMessage(
            To: invoice.Client.Email,
            ToName: invoice.Client.Name,
            Subject: EmailLayout.SubjectSafe($"Fatura {invoice.Number} de {branding.SenderName}"),
            HtmlBody: EmailLayout.Render(branding, body, $"Enviado por {branding.SenderName}"),
            Attachments: [attachment]
        ), cancellationToken);

        // Persists any newly-issued PayTokenHash/PayTokenExpiresAt from GetOrCreateToken above
        // (a no-op write if a valid token already existed and was just reused).
        await db.SaveChangesAsync(cancellationToken);

        // Opt-in WhatsApp ping. The email already succeeded and is the primary channel — this
        // is a bonus notification, so a failure here must never fail the overall command.
        // The pay link isn't re-threaded into this message — pointing back at the email (which
        // already has the "Pagar agora" button) is simpler than re-deriving it here.
        if (invoice.Client.WhatsAppOptIn && invoice.Client.PhoneVerified &&
            !string.IsNullOrWhiteSpace(invoice.Client.Phone))
        {
            try
            {
                var waMessage =
                    $"Olá {invoice.Client.Name}, {branding.SenderName} enviou-te a fatura {invoice.Number} " +
                    $"no valor de {totalFormatted}. Consulta o teu email para pagar.";

                await notificationService.SendWhatsAppAsync(invoice.Client.Phone, waMessage, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to send WhatsApp notification for invoice {InvoiceId}", request.InvoiceId);
            }
        }

        return Result.Success();
    }
}
