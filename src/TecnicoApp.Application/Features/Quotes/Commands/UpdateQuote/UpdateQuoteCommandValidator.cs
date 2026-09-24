using FluentValidation;
using TecnicoApp.Application.Features.Quotes.Commands.CreateQuote;

namespace TecnicoApp.Application.Features.Quotes.Commands.UpdateQuote;

public class UpdateQuoteCommandValidator : AbstractValidator<UpdateQuoteCommand>
{
    public UpdateQuoteCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ClientId).NotEmpty();

        RuleFor(x => x.Notes).MaximumLength(2000).When(x => x.Notes != null);

        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("O orçamento deve ter pelo menos uma linha.");

        RuleForEach(x => x.Lines).SetValidator(new QuoteLineRequestValidator());

        RuleFor(x => x.Discount)
            .GreaterThanOrEqualTo(0).When(x => x.Discount.HasValue)
            .WithMessage("O desconto não pode ser negativo.");

        RuleFor(x => x.Discount)
            .PrecisionScale(10, 2, ignoreTrailingZeros: true).When(x => x.Discount.HasValue)
            .WithMessage("O desconto admite no máximo 2 casas decimais.");

        RuleFor(x => x)
            .Must(x =>
            {
                if (!x.Discount.HasValue || x.Lines is null || x.Lines.Count == 0) return true;
                var total = x.Lines.Sum(l => l.Quantity * l.UnitPrice * (1 + l.VatRate / 100));
                return x.Discount.Value <= total;
            })
            .WithMessage("O desconto não pode ser superior ao total do orçamento.")
            .When(x => x.Discount.HasValue && x.Discount.Value > 0);
    }
}
