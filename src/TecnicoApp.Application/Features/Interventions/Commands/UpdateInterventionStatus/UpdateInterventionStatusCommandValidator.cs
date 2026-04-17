using FluentValidation;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Interventions.Commands.UpdateInterventionStatus;

public class UpdateInterventionStatusCommandValidator : AbstractValidator<UpdateInterventionStatusCommand>
{
    public UpdateInterventionStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
    }
}
