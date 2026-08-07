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

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
