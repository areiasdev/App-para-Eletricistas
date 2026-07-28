using FluentValidation;

namespace TecnicoApp.Application.Features.Invoices.Commands.UpdateInvoiceStatus;

public class UpdateInvoiceStatusCommandValidator : AbstractValidator<UpdateInvoiceStatusCommand>
{
    public UpdateInvoiceStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
    }
}
