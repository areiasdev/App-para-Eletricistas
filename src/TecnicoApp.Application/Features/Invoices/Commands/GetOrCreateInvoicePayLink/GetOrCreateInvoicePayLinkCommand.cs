using Ardalis.Result;
using MediatR;

namespace TecnicoApp.Application.Features.Invoices.Commands.GetOrCreateInvoicePayLink;

public record GetOrCreateInvoicePayLinkCommand(Guid InvoiceId) : IRequest<Result<string>>;
