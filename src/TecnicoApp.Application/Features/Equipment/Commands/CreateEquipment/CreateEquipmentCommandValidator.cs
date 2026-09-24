using FluentValidation;
using TecnicoApp.Application.Common.Security;

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
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => x.Notes != null);
        RuleForEach(x => x.Photos)
            .Must(ImageFiles.IsValidPhotoUrl)
            .WithMessage("As fotos devem ser carregadas na aplicação ou ser URLs HTTPS válidos.")
            .When(x => x.Photos is { Count: > 0 });
        RuleFor(x => x.Photos)
            .Must(p => p == null || p.Count <= 20)
            .WithMessage("Máximo de 20 fotos por equipamento.");

        RuleFor(x => x.MaintenanceIntervalMonths)
            .InclusiveBetween(1, 120).When(x => x.MaintenanceIntervalMonths.HasValue)
            .WithMessage("A periodicidade deve estar entre 1 e 120 meses.");
    }
}
