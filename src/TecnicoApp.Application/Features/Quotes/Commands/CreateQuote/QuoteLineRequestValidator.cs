using FluentValidation;

namespace TecnicoApp.Application.Features.Quotes.Commands.CreateQuote;

/// <summary>
/// Line rules shared by CreateQuote and UpdateQuote. Precision matches the database columns
/// (Quantity decimal(10,3), UnitPrice decimal(12,4)) so a value is rejected up front instead
/// of being silently rounded on save — which made the saved quote differ from the one shown.
/// </summary>
public class QuoteLineRequestValidator : AbstractValidator<CreateQuoteLineRequest>
{
    public QuoteLineRequestValidator()
    {
        RuleFor(l => l.Description)
            .NotEmpty().WithMessage("A descrição é obrigatória.")
            .MaximumLength(500);

        RuleFor(l => l.Quantity)
            .GreaterThan(0).WithMessage("A quantidade deve ser maior que zero.")
            .PrecisionScale(10, 3, ignoreTrailingZeros: true)
            .WithMessage("A quantidade admite no máximo 3 casas decimais.");

        RuleFor(l => l.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("O preço unitário não pode ser negativo.")
            .PrecisionScale(12, 4, ignoreTrailingZeros: true)
            .WithMessage("O preço unitário admite no máximo 4 casas decimais.");

        RuleFor(l => l.Unit).MaximumLength(10);

        RuleFor(l => l.VatRate)
            .InclusiveBetween(0, 100).WithMessage("A taxa de IVA deve estar entre 0 e 100.");
    }
}
