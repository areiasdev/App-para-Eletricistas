using FluentValidation;

namespace TecnicoApp.Application.Features.Team.Commands.AcceptInvite;

public class AcceptInviteCommandValidator : AbstractValidator<AcceptInviteCommand>
{
    public AcceptInviteCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().WithMessage("Token inválido.");
        RuleFor(x => x.FullName).NotEmpty().WithMessage("O nome é obrigatório.").MaximumLength(200);
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("A palavra-passe é obrigatória.")
            .MinimumLength(8).WithMessage("A palavra-passe deve ter pelo menos 8 caracteres.");
    }
}
