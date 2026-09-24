using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecnicoApp.Application.Common.Documents;
using TecnicoApp.Application.Common.Email;
using TecnicoApp.Application.Common.Formatting;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Common.Security;
using TecnicoApp.Application.Features.Quotes.DTOs;
using TecnicoApp.Application.Features.Quotes.Queries.GenerateQuotePdf;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Quotes.Public;

// Client-facing quote approval, reached only through the magic link in the quote email
// ("{BaseUrl}/orcamento/{token}"). No session, no owner resolution: the token is the credential.

/// <summary>What the client sees — no internal notes about other clients, no team data.</summary>
public record PublicQuoteDto(
    string Number,
    QuoteStatus Status,
    string ClientName,
    string IssuerName,
    string? IssuerEmail,
    string? IssuerPhone,
    string? IssuerLogoUrl,
    string? IssuerBrandColor,
    DateTime CreatedAt,
    DateTime? ValidUntil,
    string? Notes,
    IReadOnlyList<QuoteLineDto> Lines,
    decimal SubTotal,
    decimal VatTotal,
    decimal? Discount,
    decimal Total,
    DateTime? ClientDecisionAt,
    string? AcceptedByName,
    string? RejectionReason,
    bool IsExpired);

public record GetPublicQuoteQuery(string Token) : IRequest<Result<PublicQuoteDto>>;

public record AcceptQuoteOnlineCommand(string Token, string Name, string SignatureDataUrl) : IRequest<Result<PublicQuoteDto>>;

public record RejectQuoteOnlineCommand(string Token, string? Reason) : IRequest<Result<PublicQuoteDto>>;

/// <summary>PDF of the quote for the client to download from the approval page.</summary>
public record GetPublicQuotePdfQuery(string Token) : IRequest<Result<QuotePdfResult>>;

internal static class PublicQuoteLookup
{
    public static async Task<Quote?> FindByTokenAsync(IAppDbContext db, string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 200) return null;
        var hash = PublicTokens.Hash(token);
        var quote = await db.Quotes
            .Include(q => q.Lines)
            .Include(q => q.Client)
            .Include(q => q.User)
            .FirstOrDefaultAsync(q => q.ApprovalTokenHash == hash, ct);
        return quote is null || quote.ApprovalTokenExpiresAt < DateTime.UtcNow ? null : quote;
    }

    public static bool IsExpired(Quote quote) =>
        quote.ValidUntil is { } validUntil && validUntil.Date < DateTime.UtcNow.Date;

    public static PublicQuoteDto ToPublicDto(this Quote quote)
    {
        var branding = EmailBranding.ForCompany(quote.User);
        return new PublicQuoteDto(
            quote.Number, quote.Status, quote.Client.Name,
            branding.SenderName, quote.User.Email, quote.User.Phone, quote.User.LogoUrl, quote.User.BrandColor,
            quote.CreatedAt, quote.ValidUntil, quote.Notes,
            quote.Lines.ToLineDtos(), quote.SubTotal, quote.VatTotal, quote.Discount, quote.Total,
            quote.ClientDecisionAt, quote.AcceptedByName, quote.RejectionReason,
            IsExpired(quote));
    }

    /// <summary>Tells the team the client answered — best-effort, never fails the client's action.</summary>
    public static async Task NotifyTeamAsync(
        IEmailService emailService, IAppSettings appSettings, ILogger logger, Quote quote, bool accepted, CancellationToken ct)
    {
        try
        {
            var owner = quote.User;
            var branding = new EmailBranding(appSettings.ProductName);
            var verb = accepted ? "aceitou" : "recusou";
            var detail = accepted
                ? $"Assinado por <strong>{EmailLayout.Encode(quote.AcceptedByName)}</strong>. Já podes agendar a intervenção e faturar."
                : string.IsNullOrWhiteSpace(quote.RejectionReason)
                    ? "O cliente não indicou motivo."
                    : $"Motivo indicado: «{EmailLayout.Encode(quote.RejectionReason)}»";
            var body = $"""
                <h2 style="margin:0 0 16px;color:#1a1a1a;font-size:18px;">{EmailLayout.Encode(quote.Client.Name)} {verb} o orçamento {EmailLayout.Encode(quote.Number)}</h2>
                {EmailLayout.SummaryTable(("Total", PtFormat.Currency(quote.Total)))}
                <p style="margin:0 0 24px;">{detail}</p>
                {EmailLayout.Button(branding, $"{appSettings.BaseUrl}/dashboard/orcamentos/{quote.Id}", "Abrir orçamento")}
                """;
            await emailService.SendAsync(new EmailMessage(
                owner.Email, owner.FullName,
                EmailLayout.SubjectSafe($"{(accepted ? "✅" : "❌")} {quote.Client.Name} {verb} o orçamento {quote.Number}"),
                EmailLayout.Render(branding, body, appSettings.ProductName)), ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to notify team about online decision on quote {QuoteId}", quote.Id);
        }
    }
}

public class GetPublicQuoteQueryHandler(IAppDbContext db) : IRequestHandler<GetPublicQuoteQuery, Result<PublicQuoteDto>>
{
    public async Task<Result<PublicQuoteDto>> Handle(GetPublicQuoteQuery request, CancellationToken cancellationToken)
    {
        var quote = await PublicQuoteLookup.FindByTokenAsync(db, request.Token, cancellationToken);
        return quote is null ? Result.NotFound() : Result.Success(quote.ToPublicDto());
    }
}

public class AcceptQuoteOnlineCommandHandler(
    IAppDbContext db, IEmailService emailService, IAppSettings appSettings, ILogger<AcceptQuoteOnlineCommandHandler> logger)
    : IRequestHandler<AcceptQuoteOnlineCommand, Result<PublicQuoteDto>>
{
    public async Task<Result<PublicQuoteDto>> Handle(AcceptQuoteOnlineCommand request, CancellationToken cancellationToken)
    {
        var quote = await PublicQuoteLookup.FindByTokenAsync(db, request.Token, cancellationToken);
        if (quote is null) return Result.NotFound();

        if (quote.Status != QuoteStatus.Sent)
            return Result.Error("Este orçamento já não está a aguardar resposta.");
        if (PublicQuoteLookup.IsExpired(quote))
            return Result.Error("Este orçamento expirou. Pede uma versão atualizada.");
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 200)
            return Result.Invalid(new ValidationError("Name", "Indica o teu nome."));
        if (!PublicTokens.IsValidSignature(request.SignatureDataUrl))
            return Result.Invalid(new ValidationError("SignatureDataUrl", "A assinatura é obrigatória."));

        quote.Status = QuoteStatus.Accepted;
        quote.SignatureUrl = request.SignatureDataUrl;
        quote.SignedAt = quote.ClientDecisionAt = DateTime.UtcNow;
        quote.AcceptedByName = request.Name.Trim();
        quote.ModifiedBy = "cliente (online)";
        await db.SaveChangesAsync(cancellationToken);

        await PublicQuoteLookup.NotifyTeamAsync(emailService, appSettings, logger, quote, accepted: true, cancellationToken);
        return Result.Success(quote.ToPublicDto());
    }
}

public class RejectQuoteOnlineCommandHandler(
    IAppDbContext db, IEmailService emailService, IAppSettings appSettings, ILogger<RejectQuoteOnlineCommandHandler> logger)
    : IRequestHandler<RejectQuoteOnlineCommand, Result<PublicQuoteDto>>
{
    public async Task<Result<PublicQuoteDto>> Handle(RejectQuoteOnlineCommand request, CancellationToken cancellationToken)
    {
        var quote = await PublicQuoteLookup.FindByTokenAsync(db, request.Token, cancellationToken);
        if (quote is null) return Result.NotFound();

        if (quote.Status != QuoteStatus.Sent)
            return Result.Error("Este orçamento já não está a aguardar resposta.");
        if (request.Reason is { Length: > 1000 })
            return Result.Invalid(new ValidationError("Reason", "O motivo não pode exceder 1000 caracteres."));

        quote.Status = QuoteStatus.Rejected;
        quote.ClientDecisionAt = DateTime.UtcNow;
        quote.RejectionReason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        quote.ModifiedBy = "cliente (online)";
        await db.SaveChangesAsync(cancellationToken);

        await PublicQuoteLookup.NotifyTeamAsync(emailService, appSettings, logger, quote, accepted: false, cancellationToken);
        return Result.Success(quote.ToPublicDto());
    }
}

public class GetPublicQuotePdfQueryHandler(IAppDbContext db, IPdfService pdfService, IFileStorageService fileStorage)
    : IRequestHandler<GetPublicQuotePdfQuery, Result<QuotePdfResult>>
{
    public async Task<Result<QuotePdfResult>> Handle(GetPublicQuotePdfQuery request, CancellationToken cancellationToken)
    {
        var quote = await PublicQuoteLookup.FindByTokenAsync(db, request.Token, cancellationToken);
        if (quote is null) return Result.NotFound();

        var logo = await fileStorage.ReadUploadBytesAsync(quote.User.LogoUrl, cancellationToken);
        var bytes = pdfService.GenerateQuotePdf(PdfDataFactory.ForQuote(quote, quote.User, logo));
        return Result.Success(new QuotePdfResult(bytes, quote.Number));
    }
}
