using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Domain.Enums;
using TecnicoApp.Application.Common.Extensions;

namespace TecnicoApp.Application.Features.Invoices.Commands.GetOrCreateInvoicePayLink;

public class GetOrCreateInvoicePayLinkCommandHandler(
    IAppDbContext db,
    ICurrentUserService currentUser,
    IInvoicePayLinkService payLinkService,
    IAppSettings appSettings)
    : IRequestHandler<GetOrCreateInvoicePayLinkCommand, Result<string>>
{
    public async Task<Result<string>> Handle(
        GetOrCreateInvoicePayLinkCommand request, CancellationToken cancellationToken)
    {
        // Resolve ownerId: team members share their owner's invoices
        var caller = await db.ResolveCallerAsync(currentUser.UserId, cancellationToken);

        if (caller is null)
            return Result.Unauthorized();

        // Same gate as creating/managing invoices — generating a payment link for a client is
        // a company-financial action.
        if (caller.Role is not (UserRole.Owner or UserRole.Admin))
            return Result.Forbidden("Apenas o proprietário ou administradores podem gerar links de pagamento.");

        var invoice = await db.Invoices
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

        if (invoice is null)
            return Result.NotFound();

        if (invoice.UserId != caller.OwnerId)
            return Result.Forbidden();

        // Reuses an existing non-expired token rather than rotating it — see
        // IInvoicePayLinkService for how a repeat call reproduces the same link deterministically.
        var rawToken = payLinkService.GetOrCreateToken(invoice);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success($"{appSettings.BaseUrl}/pay/{rawToken}");
    }
}
