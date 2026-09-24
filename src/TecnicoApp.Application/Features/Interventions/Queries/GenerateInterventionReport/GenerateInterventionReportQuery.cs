using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Documents;
using TecnicoApp.Application.Common.Extensions;
using TecnicoApp.Application.Common.Interfaces;

namespace TecnicoApp.Application.Features.Interventions.Queries.GenerateInterventionReport;

/// <summary>Job sheet (folha de obra) PDF: what was done, materials, hours, photos, client signature.</summary>
public record GenerateInterventionReportQuery(Guid Id) : IRequest<Result<InterventionReportResult>>;

public record InterventionReportResult(byte[] Bytes, string FileName);

public class GenerateInterventionReportQueryHandler(
    IAppDbContext db, ICurrentUserService currentUser, IPdfService pdfService, IFileStorageService fileStorage)
    : IRequestHandler<GenerateInterventionReportQuery, Result<InterventionReportResult>>
{
    /// <summary>Photos embedded in the PDF — enough to document the job without a 30 MB attachment.</summary>
    public const int MaxPhotos = 6;

    public async Task<Result<InterventionReportResult>> Handle(GenerateInterventionReportQuery request, CancellationToken cancellationToken)
    {
        var ownerId = await db.ResolveOwnerIdAsync(currentUser.UserId, cancellationToken);

        var intervention = await db.Interventions
            .AsNoTracking()
            .Include(i => i.Client)
            .Include(i => i.Equipment)
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        if (intervention is null) return Result.NotFound();
        if (intervention.UserId != ownerId) return Result.Forbidden();

        var issuer = await db.Users.AsNoTracking().FirstAsync(u => u.Id == ownerId, cancellationToken);
        var technicianName = intervention.AssignedToUserId is { } techId
            ? await db.Users.AsNoTracking().Where(u => u.Id == techId).Select(u => u.FullName).FirstOrDefaultAsync(cancellationToken)
            : issuer.FullName;

        // Only photos uploaded to this install can be embedded; external URLs are skipped.
        var photos = new List<byte[]>();
        foreach (var url in intervention.Photos.Take(MaxPhotos))
            if (await fileStorage.ReadUploadBytesAsync(url, cancellationToken) is { } bytes)
                photos.Add(bytes);

        var logo = await fileStorage.ReadUploadBytesAsync(issuer.LogoUrl, cancellationToken);
        var data = PdfDataFactory.ForInterventionReport(intervention, issuer, logo, technicianName, photos);
        var pdf = pdfService.GenerateInterventionReportPdf(data);

        var date = (intervention.CompletedAt ?? intervention.ScheduledAt ?? intervention.CreatedAt).ToString("yyyy-MM-dd");
        return Result.Success(new InterventionReportResult(pdf, $"folha-de-obra-{date}-{data.Reference}.pdf"));
    }
}
