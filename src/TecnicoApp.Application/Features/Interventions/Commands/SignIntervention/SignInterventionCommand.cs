using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Extensions;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Common.Security;
using TecnicoApp.Application.Features.Interventions.DTOs;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Interventions.Commands.SignIntervention;

/// <summary>
/// Client signs the job sheet on the technician's phone at the end of the visit — proof the
/// work was done and accepted. Signing completes the job if it wasn't already.
/// </summary>
public record SignInterventionCommand(Guid Id, string SignedByName, string SignatureDataUrl) : IRequest<Result<InterventionDto>>;

public class SignInterventionCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<SignInterventionCommand, Result<InterventionDto>>
{
    public async Task<Result<InterventionDto>> Handle(SignInterventionCommand request, CancellationToken cancellationToken)
    {
        var ownerId = await db.ResolveOwnerIdAsync(currentUser.UserId, cancellationToken);

        var intervention = await db.Interventions
            .Include(i => i.Client)
            .Include(i => i.Quote)
            .Include(i => i.Equipment)
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        if (intervention is null) return Result.NotFound();
        if (intervention.UserId != ownerId) return Result.Forbidden();

        if (string.IsNullOrWhiteSpace(request.SignedByName) || request.SignedByName.Length > 200)
            return Result.Invalid(new ValidationError("SignedByName", "Indica o nome de quem assina."));
        if (!PublicTokens.IsValidSignature(request.SignatureDataUrl))
            return Result.Invalid(new ValidationError("SignatureDataUrl", "A assinatura deve ser uma imagem PNG, JPEG ou WEBP."));

        var now = DateTime.UtcNow;
        intervention.ClientSignatureUrl = request.SignatureDataUrl;
        intervention.SignedByName = request.SignedByName.Trim();
        intervention.SignedAt = now;
        if (intervention.Status != InterventionStatus.Completed)
            intervention.Complete(now);
        intervention.ModifiedBy = currentUser.Email;

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(intervention.ToDto(intervention.Client.Name, intervention.Quote?.Number));
    }
}
