using FluentValidation;

namespace TecnicoApp.Application.Features.Interventions.Commands.DeleteIntervention;

public class DeleteInterventionCommandValidator : AbstractValidator<DeleteInterventionCommand>
{
    public DeleteInterventionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
