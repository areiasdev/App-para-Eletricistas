using FluentValidation;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Team.Commands.InviteTeamMember;

public class InviteTeamMemberCommandValidator : AbstractValidator<InviteTeamMemberCommand>
{
    public InviteTeamMemberCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O email é obrigatório.")
            .EmailAddress().WithMessage("Email inválido.")
            .MaximumLength(256);

        RuleFor(x => x.Role)
            .Must(r => r is UserRole.Technician or UserRole.Admin or UserRole.Commercial)
            .WithMessage("O papel deve ser Technician, Admin ou Commercial.");
    }
}
