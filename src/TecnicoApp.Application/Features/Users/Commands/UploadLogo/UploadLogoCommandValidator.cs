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

        // The Content-Type header is attacker-controlled (multipart form field, not verified
        // by the browser) — checking it alone lets a mislabeled/malformed file through to the
        // PDF-generation image decoder later. Verify the actual file signature matches.
        RuleFor(x => x)
            .Must(x => MatchesClaimedType(x.FileContent, x.ContentType))
            .WithMessage("O ficheiro não corresponde a uma imagem válida do tipo indicado.")
            .When(x => AllowedContentTypes.Contains(x.ContentType));
    }

    private static bool MatchesClaimedType(byte[] content, string contentType) => contentType.ToLowerInvariant() switch
    {
        "image/png" => content.Length >= 8 &&
            content[0] == 0x89 && content[1] == 0x50 && content[2] == 0x4E && content[3] == 0x47 &&
            content[4] == 0x0D && content[5] == 0x0A && content[6] == 0x1A && content[7] == 0x0A,
        "image/jpeg" => content.Length >= 3 &&
            content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF,
        "image/webp" => content.Length >= 12 &&
            content[0] == 'R' && content[1] == 'I' && content[2] == 'F' && content[3] == 'F' &&
            content[8] == 'W' && content[9] == 'E' && content[10] == 'B' && content[11] == 'P',
        _ => false,
    };
}
