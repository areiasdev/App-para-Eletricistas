using FluentValidation;

namespace TecnicoApp.Application.Features.Users.Commands.UpdateProfile;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("O nome é obrigatório.")
            .MaximumLength(200).WithMessage("O nome não pode ter mais de 200 caracteres.");

        RuleFor(x => x.CompanyName)
            .MaximumLength(200).WithMessage("O nome da empresa não pode ter mais de 200 caracteres.")
            .When(x => x.CompanyName is not null);

        RuleFor(x => x.Nif)
            .Matches(@"^\d{9}$").WithMessage("O NIF deve ter 9 dígitos.")
            .When(x => !string.IsNullOrEmpty(x.Nif));

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("O telemóvel não pode ter mais de 20 caracteres.")
            .When(x => x.Phone is not null);

        RuleFor(x => x.BrandColor)
            .Matches(@"^#[0-9a-fA-F]{6}$").WithMessage("A cor deve ser um código hexadecimal (ex: #f59e0b).")
            .When(x => !string.IsNullOrEmpty(x.BrandColor));

        // Portugal-only for now.
        RuleFor(x => x.Iban)
            .Matches(@"^PT50\d{21}$").WithMessage("IBAN inválido. Deve começar por PT50 seguido de 21 dígitos.")
            .When(x => !string.IsNullOrEmpty(x.Iban));

        RuleFor(x => x.BankName)
            .MaximumLength(100).WithMessage("O nome do banco não pode ter mais de 100 caracteres.")
            .When(x => x.BankName is not null);
    }
}
