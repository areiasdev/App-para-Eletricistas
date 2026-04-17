using FluentValidation;

namespace TecnicoApp.Application.Features.Clients.Commands.DeleteClient;

public class DeleteClientCommandValidator : AbstractValidator<DeleteClientCommand>
{
    public DeleteClientCommandValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
    }
}
