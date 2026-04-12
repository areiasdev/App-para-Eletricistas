using FluentValidation;

namespace TecnicoApp.Application.Features.Clients.Commands.CreateClient;

public class CreateClientCommandValidator : AbstractValidator<CreateClientCommand>
{
    public CreateClientCommandValidator()
    {
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

        RuleFor(x => x.Phone)
            .MaximumLength(20)
            .When(x => !string.IsNullOrEmpty(x.Phone));

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
