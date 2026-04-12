using FluentValidation;

namespace TecnicoApp.Application.Features.Quotes.Commands.CreateQuote;

public class CreateQuoteCommandValidator : AbstractValidator<CreateQuoteCommand>
{
    public CreateQuoteCommandValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();

        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("O orçamento deve ter pelo menos uma linha.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Description)
                .NotEmpty().WithMessage("A descrição é obrigatória.")
                .MaximumLength(500);

            line.RuleFor(l => l.Quantity)
                .GreaterThan(0).WithMessage("A quantidade deve ser maior que zero.");

            line.RuleFor(l => l.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("O preço unitário não pode ser negativo.");

            line.RuleFor(l => l.VatRate)
                .InclusiveBetween(0, 100).WithMessage("A taxa de IVA deve estar entre 0 e 100.");
        });

        RuleFor(x => x.Discount)
            .GreaterThanOrEqualTo(0).When(x => x.Discount.HasValue)
            .WithMessage("O desconto não pode ser negativo.");
    }
}
