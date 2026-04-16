using FluentValidation;

namespace TecnicoApp.Application.Features.Equipment.Commands.CreateEquipment;

public class CreateEquipmentCommandValidator : AbstractValidator<CreateEquipmentCommand>
{
    public CreateEquipmentCommandValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("O tipo de equipamento é obrigatório.")
            .MaximumLength(100);
        RuleFor(x => x.Brand).MaximumLength(100).When(x => x.Brand != null);
        RuleFor(x => x.Model).MaximumLength(100).When(x => x.Model != null);
        RuleFor(x => x.SerialNumber).MaximumLength(100).When(x => x.SerialNumber != null);
        RuleForEach(x => x.Photos)
            .Must(url =>
                Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttps || uri.Host == "localhost"))
            .WithMessage("As fotos devem ser URLs HTTPS válidos.")
            .MaximumLength(2048)
            .When(x => x.Photos is { Count: > 0 });
        RuleFor(x => x.Photos)
            .Must(p => p == null || p.Count <= 20)
            .WithMessage("Máximo de 20 fotos por equipamento.");
    }
}
