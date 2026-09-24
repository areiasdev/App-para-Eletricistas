using Ardalis.Result;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Extensions;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Common.Security;

namespace TecnicoApp.Application.Features.Uploads;

/// <summary>
/// A photo taken on site (before/after, meter, fault, installed equipment). Returns the URL
/// path to store in an intervention's or equipment's Photos list.
/// </summary>
public record UploadPhotoCommand(byte[] FileContent, string ContentType) : IRequest<Result<string>>;

public class UploadPhotoCommandValidator : AbstractValidator<UploadPhotoCommand>
{
    /// <summary>Phones shoot 3–8 MB JPEGs; the web app downsizes before upload, this is the hard cap.</summary>
    public const int MaxSizeBytes = 10 * 1024 * 1024;

    public UploadPhotoCommandValidator()
    {
        RuleFor(x => x.ContentType)
            .Must(ImageFiles.IsAllowedContentType)
            .WithMessage("A foto deve ser uma imagem PNG, JPEG ou WEBP.");

        RuleFor(x => x.FileContent)
            .NotEmpty().WithMessage("Ficheiro vazio.")
            .Must(c => c.Length <= MaxSizeBytes).WithMessage("A foto não pode exceder 10MB.");

        RuleFor(x => x)
            .Must(x => ImageFiles.MatchesClaimedType(x.FileContent, x.ContentType))
            .WithMessage("O ficheiro não corresponde a uma imagem válida do tipo indicado.")
            .When(x => ImageFiles.IsAllowedContentType(x.ContentType));
    }
}

public class UploadPhotoCommandHandler(IAppDbContext db, ICurrentUserService currentUser, IFileStorageService fileStorage)
    : IRequestHandler<UploadPhotoCommand, Result<string>>
{
    public async Task<Result<string>> Handle(UploadPhotoCommand command, CancellationToken cancellationToken)
    {
        var ownerId = await db.ResolveOwnerIdAsync(currentUser.UserId, cancellationToken);
        if (ownerId == Guid.Empty || !await db.Users.AnyAsync(u => u.Id == ownerId, cancellationToken))
            return Result.Unauthorized();

        var url = await fileStorage.SavePhotoAsync(
            ownerId, command.FileContent, ImageFiles.ExtensionsByContentType[command.ContentType], cancellationToken);
        return Result.Success(url);
    }
}
