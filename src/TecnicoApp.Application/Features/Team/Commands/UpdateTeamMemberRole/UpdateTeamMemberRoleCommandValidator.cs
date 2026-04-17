using FluentValidation;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Team.Commands.UpdateTeamMemberRole;

public class UpdateTeamMemberRoleCommandValidator : AbstractValidator<UpdateTeamMemberRoleCommand>
{
    public UpdateTeamMemberRoleCommandValidator()
    {
        RuleFor(x => x.TeamMemberId).NotEmpty();
        RuleFor(x => x.Role)
            .IsInEnum()
            .NotEqual(UserRole.Owner)
            .WithMessage("O papel Owner não pode ser atribuído através desta operação.");
    }
}
