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

        RuleFor(x => x.LogoUrl)
            .MaximumLength(500).WithMessage("URL do logótipo demasiado longo.")
            .When(x => x.LogoUrl is not null);
    }
}
