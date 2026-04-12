using FluentValidation;

namespace TecnicoApp.Application.Features.Interventions.Commands.UpdateIntervention;

public class UpdateInterventionCommandValidator : AbstractValidator<UpdateInterventionCommand>
{
    public UpdateInterventionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("O título é obrigatório.")
            .MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(5000).When(x => x.Description != null);
        RuleFor(x => x.TechnicianNotes).MaximumLength(5000).When(x => x.TechnicianNotes != null);
    }
}
