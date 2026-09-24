using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Invoices.DTOs;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.Application.Common.Extensions;

namespace TecnicoApp.Application.Features.Invoices.Commands.CreateInvoiceFromQuote;

public class CreateInvoiceFromQuoteCommandHandler(
    IAppDbContext db,
    ICurrentUserService currentUser,
    IAppSettings appSettings,
    ILogger<CreateInvoiceFromQuoteCommandHandler> logger)
    : IRequestHandler<CreateInvoiceFromQuoteCommand, Result<InvoiceDto>>
{
    public async Task<Result<InvoiceDto>> Handle(
        CreateInvoiceFromQuoteCommand request, CancellationToken cancellationToken)
    {
        // Resolve ownerId: team members share their owner's quotes/invoices
        var caller = await db.ResolveCallerAsync(currentUser.UserId, cancellationToken);

        if (caller is null)
            return Result.Unauthorized();

        // Issuing (and later cancelling) an invoice is a company-financial action —
        // restrict to Owner/Admin, same gate as deleting a quote.
        if (caller.Role is not (UserRole.Owner or UserRole.Admin))
            return Result.Forbidden("Apenas o proprietário ou administradores podem faturar orçamentos.");

        var ownerId = caller.OwnerId;

        var quote = await db.Quotes
            .Include(q => q.Lines)
            .Include(q => q.Client)
            .FirstOrDefaultAsync(q => q.Id == request.QuoteId, cancellationToken);

        if (quote is null)
            return Result.NotFound();

        if (quote.UserId != ownerId)
            return Result.Forbidden();

        if (quote.Status != QuoteStatus.Accepted)
            return Result.Error("Só é possível faturar orçamentos aceites.");

        var alreadyInvoiced = await db.Invoices
            .AnyAsync(i => i.QuoteId == quote.Id && i.Status != InvoiceStatus.Cancelled, cancellationToken);

        if (alreadyInvoiced)
            return Result.Error("Este orçamento já foi faturado.");

        var issuedAt = DateTime.UtcNow;

        var invoice = new Invoice
        {
            Number = string.Empty, // assigned by AddInvoiceWithNextNumberAsync (FT-YYYY-NNNN)
            IssuedAt = issuedAt,
            DueDate = issuedAt.AddDays(appSettings.InvoicePaymentTermDays),
            Notes = quote.Notes,
            Discount = quote.Discount,
            QuoteId = quote.Id,
            ClientId = quote.ClientId,
            UserId = ownerId,
            Lines = quote.Lines.Select(l => new InvoiceLine
            {
                Description = l.Description,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                VatRate = l.VatRate,
                Unit = l.Unit,
                Position = l.Position,
            }).ToList(),
        };

        quote.Status = QuoteStatus.Invoiced;

        await db.AddInvoiceWithNextNumberAsync(invoice, ownerId, logger, cancellationToken);

        var dto = invoice.ToDto(quote.Client.Name, quote.Number);

        return Result.Success(dto);
    }
}
