using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Invoices.DTOs;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Invoices.Commands.CreateInvoiceFromQuote;

public class CreateInvoiceFromQuoteCommandHandler(
    IAppDbContext db,
    ICurrentUserService currentUser,
    ILogger<CreateInvoiceFromQuoteCommandHandler> logger)
    : IRequestHandler<CreateInvoiceFromQuoteCommand, Result<InvoiceDto>>
{
    public async Task<Result<InvoiceDto>> Handle(
        CreateInvoiceFromQuoteCommand request, CancellationToken cancellationToken)
    {
        // Resolve ownerId: team members share their owner's quotes/invoices
        var caller = await db.Users.AsNoTracking()
            .Where(u => u.Id == currentUser.UserId)
            .Select(u => new { OwnerId = u.OwnerId ?? u.Id, u.Role })
            .FirstOrDefaultAsync(cancellationToken);

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

        // Generate invoice number: FT-YYYY-NNNN
        var year = DateTime.UtcNow.Year;
        var count = await db.Invoices
            .CountAsync(i => i.UserId == ownerId && i.CreatedAt.Year == year, cancellationToken);
        var number = $"FT-{year}-{(count + 1):D4}";

        var issuedAt = DateTime.UtcNow;

        var invoice = new Invoice
        {
            Number = number,
            IssuedAt = issuedAt,
            DueDate = issuedAt.AddDays(30),
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
            }).ToList(),
        };

        quote.Status = QuoteStatus.Invoiced;

        db.Invoices.Add(invoice);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Invoices_Number", StringComparison.OrdinalIgnoreCase) == true)
        {
            // Unique constraint violation on Number — concurrent request generated same number
            logger.LogWarning("Invoice number conflict for {Number}, retrying.", number);
            db.Invoices.Remove(invoice);
            var retryCount = await db.Invoices
                .CountAsync(i => i.UserId == ownerId && i.CreatedAt.Year == year, cancellationToken);
            invoice.Number = $"FT-{year}-{(retryCount + 1):D4}";
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync(cancellationToken);
        }

        var dto = new InvoiceDto(
            invoice.Id,
            invoice.Number,
            invoice.Status,
            invoice.Discount,
            invoice.Notes,
            invoice.IssuedAt,
            invoice.DueDate,
            invoice.PaidAt,
            invoice.ClientId,
            quote.Client.Name,
            invoice.QuoteId,
            quote.Number,
            invoice.SubTotal,
            invoice.VatTotal,
            invoice.Total,
            invoice.Lines
                .Select(l => new InvoiceLineDto(
                    l.Id, l.Description, l.Quantity, l.UnitPrice, l.VatRate,
                    Math.Round(l.Quantity * l.UnitPrice * (1 + l.VatRate / 100), 2, MidpointRounding.AwayFromZero)))
                .ToList(),
            invoice.CreatedAt
        );

        return Result.Success(dto);
    }
}
