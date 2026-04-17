using FluentValidation;

namespace TecnicoApp.Application.Features.Team.Commands.RemoveTeamMember;

public class RemoveTeamMemberCommandValidator : AbstractValidator<RemoveTeamMemberCommand>
{
    public RemoveTeamMemberCommandValidator()
    {
        RuleFor(x => x.TeamMemberId).NotEmpty();
    }
}
