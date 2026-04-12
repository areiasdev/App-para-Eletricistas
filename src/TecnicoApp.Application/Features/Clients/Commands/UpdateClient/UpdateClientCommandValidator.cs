using FluentValidation;
using TecnicoApp.Application.Features.Clients.Commands.CreateClient;

namespace TecnicoApp.Application.Features.Clients.Commands.UpdateClient;

public class UpdateClientCommandValidator : AbstractValidator<UpdateClientCommand>
{
    public UpdateClientCommandValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome é obrigatório.")
            .MaximumLength(200);

        RuleFor(x => x.Nif)
            .Length(9).WithMessage("O NIF deve ter 9 dígitos.")
            .Matches(@"^\d{9}$").WithMessage("O NIF deve conter apenas dígitos.")
            .When(x => !string.IsNullOrEmpty(x.Nif));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("O email não é válido.")
            .MaximumLength(256)
            .When(x => !string.IsNullOrEmpty(x.Email));

        When(x => x.Address is not null, () =>
        {
            RuleFor(x => x.Address!.Street).NotEmpty().MaximumLength(300);
            RuleFor(x => x.Address!.City).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Address!.PostalCode)
                .NotEmpty()
                .Matches(@"^\d{4}-\d{3}$").WithMessage("O código postal deve ter o formato 0000-000.");
        });
    }
}
