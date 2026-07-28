using FluentValidation;

namespace TecnicoApp.Application.Features.Invoices.Commands.CreateInvoiceFromQuote;

public class CreateInvoiceFromQuoteCommandValidator : AbstractValidator<CreateInvoiceFromQuoteCommand>
{
    public CreateInvoiceFromQuoteCommandValidator()
    {
        RuleFor(x => x.QuoteId).NotEmpty();
    }
}
