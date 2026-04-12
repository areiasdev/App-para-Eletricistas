using FluentValidation;

namespace TecnicoApp.Application.Features.Interventions.Commands.CreateIntervention;

public class CreateInterventionCommandValidator : AbstractValidator<CreateInterventionCommand>
{
    public CreateInterventionCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("O título é obrigatório.")
            .MaximumLength(300);
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(5000).When(x => x.Description != null);
    }
}
