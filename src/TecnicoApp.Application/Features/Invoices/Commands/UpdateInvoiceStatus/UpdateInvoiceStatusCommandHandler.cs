using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Domain.Enums;
using TecnicoApp.Application.Common.Extensions;

namespace TecnicoApp.Application.Features.Invoices.Commands.UpdateInvoiceStatus;

public class UpdateInvoiceStatusCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpdateInvoiceStatusCommand, Result>
{
    public async Task<Result> Handle(
        UpdateInvoiceStatusCommand request, CancellationToken cancellationToken)
    {
        // Resolve ownerId: team members share their owner's invoices
        var caller = await db.ResolveCallerAsync(currentUser.UserId, cancellationToken);

        if (caller is null)
            return Result.Unauthorized();

        // Marking paid/overdue/cancelled is a company-financial action — Owner/Admin only,
        // same gate as creating the invoice.
        if (caller.Role is not (UserRole.Owner or UserRole.Admin))
            return Result.Forbidden("Apenas o proprietário ou administradores podem alterar o estado de faturas.");

        var ownerId = caller.OwnerId;

        var invoice = await db.Invoices
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        if (invoice is null)
            return Result.NotFound();

        if (invoice.UserId != ownerId)
            return Result.Forbidden();

        // Validate status transitions
        var valid = (invoice.Status, request.Status) switch
        {
            (InvoiceStatus.Issued, InvoiceStatus.Paid) => true,
            (InvoiceStatus.Issued, InvoiceStatus.Cancelled) => true,
            (InvoiceStatus.Issued, InvoiceStatus.Overdue) => true,
            (InvoiceStatus.Overdue, InvoiceStatus.Paid) => true,
            (InvoiceStatus.Overdue, InvoiceStatus.Cancelled) => true,
            _ => false
        };

        if (!valid)
            return Result.Error($"Transição de estado inválida: {invoice.Status} → {request.Status}.");

        invoice.Status = request.Status;
        invoice.ModifiedBy = currentUser.Email;

        if (request.Status == InvoiceStatus.Paid)
            invoice.PaidAt = DateTime.UtcNow;

        // Cancelling the invoice releases its source quote so it can be invoiced again
        // (e.g. to fix a wrong line). Without this the quote stayed "Invoiced" forever and
        // CreateInvoiceFromQuote — which requires Accepted — could never re-issue it.
        if (request.Status == InvoiceStatus.Cancelled && invoice.QuoteId is { } quoteId)
        {
            var quote = await db.Quotes.FirstOrDefaultAsync(q => q.Id == quoteId, cancellationToken);
            if (quote is { Status: QuoteStatus.Invoiced })
            {
                quote.Status = QuoteStatus.Accepted;
                quote.ModifiedBy = currentUser.Email;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
