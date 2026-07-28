using Ardalis.Result;
using MediatR;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Invoices.Commands.UpdateInvoiceStatus;

public record UpdateInvoiceStatusCommand(Guid Id, InvoiceStatus Status) : IRequest<Result>;
