using FluentValidation;

namespace TecnicoApp.Application.Features.Quotes.Commands.UpdateQuoteStatus;

public class UpdateQuoteStatusCommandValidator : AbstractValidator<UpdateQuoteStatusCommand>
{
    public UpdateQuoteStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
    }
}
