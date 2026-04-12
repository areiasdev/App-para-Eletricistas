using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Quotes.DTOs;

namespace TecnicoApp.Application.Features.Quotes.Commands.SendQuoteEmail;

public class SendQuoteEmailCommandHandler(
    IAppDbContext db,
    ICurrentUserService currentUser,
    IPdfService pdfService,
    IEmailService emailService)
    : IRequestHandler<SendQuoteEmailCommand, Result>
{
    public async Task<Result> Handle(SendQuoteEmailCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var quote = await db.Quotes
            .Include(q => q.Lines)
            .Include(q => q.Client)
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == request.QuoteId, cancellationToken);

        if (quote is null) return Result.NotFound();
        if (quote.UserId != userId) return Result.Forbidden();

        if (string.IsNullOrWhiteSpace(quote.Client?.Email))
            return Result.Invalid(new ValidationError(
                "ClientEmail",
                "O cliente não tem email registado. Adiciona um email ao cliente para poder enviar o orçamento."));

        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null) return Result.Unauthorized();

        // Generate PDF
        var lineDtos = quote.Lines.Select(l => new QuoteLineDto(
            l.Id, l.Description, l.Quantity, l.UnitPrice, l.VatRate,
            l.Quantity * l.UnitPrice * (1 + l.VatRate / 100))).ToList();

        var pdfData = new QuotePdfData(
            Number: quote.Number,
            CreatedAt: quote.CreatedAt,
            ValidUntil: quote.ValidUntil,
            Notes: quote.Notes,
            ClientName: quote.Client.Name,
            ClientEmail: quote.Client.Email,
            ClientPhone: quote.Client.Phone,
            ClientNif: null,
            IssuerName: user.FullName,
            IssuerCompany: user.CompanyName,
            IssuerEmail: user.Email,
            IssuerPhone: user.Phone,
            IssuerNif: user.Nif,
            Lines: lineDtos,
            SubTotal: quote.SubTotal,
            VatTotal: quote.VatTotal,
            Discount: quote.Discount,
            Total: quote.Total
        );

        byte[] pdfBytes;
        try { pdfBytes = pdfService.GenerateQuotePdf(pdfData); }
        catch { return Result.Error("Não foi possível gerar o PDF do orçamento."); }

        // Build HTML email
        var issuerDisplay = user.CompanyName ?? user.FullName;
        var totalFormatted = quote.Total.ToString("C", new System.Globalization.CultureInfo("pt-PT"));
        var validStr = quote.ValidUntil.HasValue
            ? quote.ValidUntil.Value.ToString("d 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("pt-PT"))
            : "—";

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
                        <span style="color:#f59e0b;font-size:20px;font-weight:700;">⚡ TécnicoApp</span>
                      </td>
                    </tr>
                    <tr><td style="padding:32px;">
                      <p style="margin:0 0 8px;color:#6b7280;font-size:13px;font-weight:600;text-transform:uppercase;letter-spacing:.05em;">Orçamento</p>
                      <p style="margin:0 0 24px;color:#1a1a1a;font-size:28px;font-weight:700;font-family:monospace;">{quote.Number}</p>
                      <p style="margin:0 0 16px;color:#374151;font-size:15px;">Olá {quote.Client.Name},</p>
                      <p style="margin:0 0 24px;color:#374151;font-size:15px;line-height:1.6;">
                        <strong>{issuerDisplay}</strong> enviou-te um orçamento. Encontras o PDF em anexo com todos os detalhes.
                      </p>
                      <table width="100%" cellpadding="0" cellspacing="0" style="background:#f9fafb;border-radius:8px;border:1px solid #e5e7eb;margin-bottom:24px;">
                        <tr><td style="padding:16px 20px;">
                          <table width="100%">
                            <tr>
                              <td style="color:#6b7280;font-size:13px;padding:4px 0;">Total</td>
                              <td align="right" style="color:#1a1a1a;font-size:16px;font-weight:700;padding:4px 0;">{totalFormatted}</td>
                            </tr>
                            <tr>
                              <td style="color:#6b7280;font-size:13px;padding:4px 0;">Válido até</td>
                              <td align="right" style="color:#374151;font-size:13px;padding:4px 0;">{validStr}</td>
                            </tr>
                          </table>
                        </td></tr>
                      </table>
                      <p style="margin:0 0 8px;color:#6b7280;font-size:13px;line-height:1.6;">
                        Para qualquer questão, contacta diretamente <strong>{issuerDisplay}</strong>.
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
            FileName: $"orcamento-{quote.Number}.pdf",
            Content: pdfBytes,
            ContentType: "application/pdf");

        await emailService.SendAsync(new EmailMessage(
            To: quote.Client.Email,
            ToName: quote.Client.Name,
            Subject: $"Orçamento {quote.Number} de {issuerDisplay}",
            HtmlBody: html,
            Attachments: [attachment]
        ), cancellationToken);

        return Result.Success();
    }
}
