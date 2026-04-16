using FluentValidation;

namespace TecnicoApp.Application.Features.Auth.Commands.ResetPassword;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O email é obrigatório.")
            .EmailAddress().WithMessage("O email não é válido.");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("O token é obrigatório.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("A password é obrigatória.")
            .MinimumLength(8).WithMessage("A password deve ter pelo menos 8 caracteres.")
            .Matches(@"[A-Z]").WithMessage("A password deve ter pelo menos uma letra maiúscula.")
            .Matches(@"[0-9]").WithMessage("A password deve ter pelo menos um número.");
    }
}
