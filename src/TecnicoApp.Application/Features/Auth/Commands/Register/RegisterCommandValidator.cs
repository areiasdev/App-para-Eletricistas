using FluentValidation;

namespace TecnicoApp.Application.Features.Auth.Commands.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("O nome é obrigatório.")
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O email é obrigatório.")
            .EmailAddress().WithMessage("O email não é válido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("A password é obrigatória.")
            .MinimumLength(8).WithMessage("A password deve ter pelo menos 8 caracteres.")
            .Matches(@"[A-Z]").WithMessage("A password deve ter pelo menos uma letra maiúscula.")
            .Matches(@"[0-9]").WithMessage("A password deve ter pelo menos um número.");
    }
}
