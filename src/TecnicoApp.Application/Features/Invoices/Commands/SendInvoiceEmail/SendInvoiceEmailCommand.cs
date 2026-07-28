using Ardalis.Result;
using MediatR;

namespace TecnicoApp.Application.Features.Invoices.Commands.SendInvoiceEmail;

public record SendInvoiceEmailCommand(Guid InvoiceId) : IRequest<Result>;
