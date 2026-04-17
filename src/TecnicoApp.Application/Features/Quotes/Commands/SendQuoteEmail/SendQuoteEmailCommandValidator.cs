using FluentValidation;

namespace TecnicoApp.Application.Features.Quotes.Commands.SendQuoteEmail;

public class SendQuoteEmailCommandValidator : AbstractValidator<SendQuoteEmailCommand>
{
    public SendQuoteEmailCommandValidator()
    {
        RuleFor(x => x.QuoteId).NotEmpty();
    }
}
