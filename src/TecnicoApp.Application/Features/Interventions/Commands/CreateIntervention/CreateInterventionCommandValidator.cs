using FluentValidation;

namespace TecnicoApp.Application.Features.Interventions.Commands.CreateIntervention;

public class CreateInterventionCommandValidator : AbstractValidator<CreateInterventionCommand>
{
    public CreateInterventionCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("O título é obrigatório.")
            .MaximumLength(300);
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(5000).When(x => x.Description != null);
        RuleForEach(x => x.Photos)
            .Must(url =>
                Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttps || uri.Host == "localhost"))
            .WithMessage("As fotos devem ser URLs HTTPS válidos.")
            .MaximumLength(2048)
            .When(x => x.Photos is { Count: > 0 });
        RuleFor(x => x.Photos)
            .Must(p => p == null || p.Count <= 20)
            .WithMessage("Máximo de 20 fotos por intervenção.")
            .Must(p => p == null || p.Distinct().Count() == p.Count)
            .WithMessage("Não são permitidas fotos duplicadas.");

        RuleForEach(x => x.Materials)
            .ChildRules(m =>
            {
                m.RuleFor(x => x.Name).NotEmpty().WithMessage("O nome do material é obrigatório.").MaximumLength(200);
                m.RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("A quantidade deve ser maior que zero.");
                m.RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0).WithMessage("O custo unitário não pode ser negativo.");
            })
            .When(x => x.Materials is { Count: > 0 });
    }
}
