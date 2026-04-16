using FluentValidation;

namespace TecnicoApp.Application.Features.Equipment.Queries.GetEquipment;

public class GetEquipmentQueryValidator : AbstractValidator<GetEquipmentQuery>
{
    public GetEquipmentQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("A página deve ser maior que zero.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("O tamanho da página deve estar entre 1 e 100.");
    }
}
