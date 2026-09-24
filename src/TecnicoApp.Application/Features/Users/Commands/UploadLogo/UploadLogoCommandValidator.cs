using FluentValidation;
using TecnicoApp.Application.Common.Security;

namespace TecnicoApp.Application.Features.Users.Commands.UploadLogo;

public class UploadLogoCommandValidator : AbstractValidator<UploadLogoCommand>
{
    private const int MaxSizeBytes = 2 * 1024 * 1024; // 2MB

    public UploadLogoCommandValidator()
    {
        RuleFor(x => x.ContentType)
            .Must(ImageFiles.IsAllowedContentType)
            .WithMessage("O logótipo deve ser uma imagem PNG, JPEG ou WEBP.");

        RuleFor(x => x.FileContent)
            .NotEmpty().WithMessage("Ficheiro vazio.")
            .Must(c => c.Length <= MaxSizeBytes).WithMessage("O logótipo não pode exceder 2MB.");

        RuleFor(x => x)
            .Must(x => ImageFiles.MatchesClaimedType(x.FileContent, x.ContentType))
            .WithMessage("O ficheiro não corresponde a uma imagem válida do tipo indicado.")
            .When(x => ImageFiles.IsAllowedContentType(x.ContentType));
    }
}
