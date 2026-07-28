using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Invoices.DTOs;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Invoices.Commands.SendInvoiceEmail;

public class SendInvoiceEmailCommandHandler(
    IAppDbContext db,
    ICurrentUserService currentUser,
    IPdfService pdfService,
    IEmailService emailService,
    IFileStorageService fileStorage,
    IInvoicePayLinkService payLinkService,
    IAppSettings appSettings,
    ILogger<SendInvoiceEmailCommandHandler> logger)
    : IRequestHandler<SendInvoiceEmailCommand, Result>
{
    public async Task<Result> Handle(SendInvoiceEmailCommand request, CancellationToken cancellationToken)
    {
        // Resolve ownerId: team members share their owner's invoices
        var caller = await db.Users.AsNoTracking()
            .Where(u => u.Id == currentUser.UserId)
            .Select(u => new { OwnerId = u.OwnerId ?? u.Id, u.Role })
            .FirstOrDefaultAsync(cancellationToken);

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

        var lineDtos = invoice.Lines
            .OrderBy(l => l.CreatedAt)
            .Select(l => new InvoiceLineDto(
                l.Id, l.Description, l.Quantity, l.UnitPrice, l.VatRate,
                Math.Round(l.Quantity * l.UnitPrice * (1 + l.VatRate / 100), 2, MidpointRounding.AwayFromZero)))
            .ToList();

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

        // Build HTML email — HTML-encode user-supplied strings to prevent injection
        var issuerDisplay = System.Net.WebUtility.HtmlEncode(user.CompanyName ?? user.FullName);
        var clientDisplayName = System.Net.WebUtility.HtmlEncode(invoice.Client.Name);
        var totalFormatted = invoice.Total.ToString("C", new System.Globalization.CultureInfo("pt-PT"));
        var dueStr = invoice.DueDate.ToString("d 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("pt-PT"));

        var payButtonHtml = payUrl is not null
            ? $"""
              <div style="text-align:center;margin:8px 0 24px;">
                <a href="{payUrl}"
                  style="display:inline-block;background:#f59e0b;color:#1c1917;font-weight:700;
                    font-size:15px;padding:12px 28px;border-radius:8px;text-decoration:none">
                  Pagar agora →
                </a>
              </div>
              """
            : "";

        var html = $"""
            <!DOCTYPE html>
            <html lang="pt">
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
            <body style="margin:0;padding:0;background:#f7f7f4;font-family:'Helvetica Neue',Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background:#f7f7f4;padding:40px 0;">
                <tr><td align="center">
                  <table width="560" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:12px;overflow:hidden;border:1px solid #e5e7eb;">
                    <tr>
                      <td style="background:#17171a;padding:24px 32px;text-align:center;">
                        <span style="color:#f59e0b;font-size:20px;font-weight:700;">T TécnicoApp</span>
                      </td>
                    </tr>
                    <tr><td style="padding:32px;">
                      <p style="margin:0 0 8px;color:#6b7280;font-size:13px;font-weight:600;text-transform:uppercase;letter-spacing:.05em;">Fatura</p>
                      <p style="margin:0 0 24px;color:#1a1a1a;font-size:28px;font-weight:700;font-family:monospace;">{invoice.Number}</p>
                      <p style="margin:0 0 16px;color:#374151;font-size:15px;">Olá {clientDisplayName},</p>
                      <p style="margin:0 0 24px;color:#374151;font-size:15px;line-height:1.6;">
                        <strong>{issuerDisplay}</strong> enviou-te uma fatura. Encontras o PDF em anexo com todos os detalhes.
                      </p>
                      <table width="100%" cellpadding="0" cellspacing="0" style="background:#f9fafb;border-radius:8px;border:1px solid #e5e7eb;margin-bottom:24px;">
                        <tr><td style="padding:16px 20px;">
                          <table width="100%">
                            <tr>
                              <td style="color:#6b7280;font-size:13px;padding:4px 0;">Total</td>
                              <td align="right" style="color:#1a1a1a;font-size:16px;font-weight:700;padding:4px 0;">{totalFormatted}</td>
                            </tr>
                            <tr>
                              <td style="color:#6b7280;font-size:13px;padding:4px 0;">Vencimento</td>
                              <td align="right" style="color:#374151;font-size:13px;padding:4px 0;">{dueStr}</td>
                            </tr>
                          </table>
                        </td></tr>
                      </table>
                      {payButtonHtml}
                      <p style="margin:0 0 8px;color:#6b7280;font-size:13px;line-height:1.6;">
                        Para qualquer questão, contacta diretamente <strong>{issuerDisplay}</strong>.
                        <!-- issuerDisplay and clientDisplayName are HTML-encoded -->
                      </p>
                    </td></tr>
                    <tr>
                      <td style="background:#f9fafb;padding:16px 32px;text-align:center;border-top:1px solid #e5e7eb;">
                        <p style="margin:0;color:#9ca3af;font-size:11px;">Enviado via TécnicoApp &middot; tecnicoapp.pt</p>
                      </td>
                    </tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;

        var attachment = new EmailAttachment(
            FileName: $"fatura-{invoice.Number}.pdf",
            Content: pdfBytes,
            ContentType: "application/pdf");

        // Use plain issuerDisplay (non-HTML-encoded) for plain-text subject
        var issuerPlain = user.CompanyName ?? user.FullName;
        await emailService.SendAsync(new EmailMessage(
            To: invoice.Client.Email,
            ToName: invoice.Client.Name,
            Subject: $"Fatura {invoice.Number} de {issuerPlain.Replace('\n', ' ').Replace('\r', ' ')}",
            HtmlBody: html,
            Attachments: [attachment]
        ), cancellationToken);

        // Persists any newly-issued PayTokenHash/PayTokenExpiresAt from GetOrCreateToken above
        // (a no-op write if a valid token already existed and was just reused).
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
