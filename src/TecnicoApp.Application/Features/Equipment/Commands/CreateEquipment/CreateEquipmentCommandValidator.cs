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
    }
}
