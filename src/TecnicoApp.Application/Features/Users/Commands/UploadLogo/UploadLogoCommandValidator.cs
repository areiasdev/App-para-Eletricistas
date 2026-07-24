using FluentValidation;

namespace TecnicoApp.Application.Features.Users.Commands.UploadLogo;

public class UploadLogoCommandValidator : AbstractValidator<UploadLogoCommand>
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/webp"
    };

    private const int MaxSizeBytes = 2 * 1024 * 1024; // 2MB

    public UploadLogoCommandValidator()
    {
        RuleFor(x => x.ContentType)
            .Must(ct => AllowedContentTypes.Contains(ct))
            .WithMessage("O logótipo deve ser uma imagem PNG, JPEG ou WEBP.");

        RuleFor(x => x.FileContent)
            .NotEmpty().WithMessage("Ficheiro vazio.")
            .Must(c => c.Length <= MaxSizeBytes).WithMessage("O logótipo não pode exceder 2MB.");
    }
}
