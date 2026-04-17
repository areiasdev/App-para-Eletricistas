using FluentValidation;

namespace TecnicoApp.Application.Features.Equipment.Commands.DeleteEquipment;

public class DeleteEquipmentCommandValidator : AbstractValidator<DeleteEquipmentCommand>
{
    public DeleteEquipmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
