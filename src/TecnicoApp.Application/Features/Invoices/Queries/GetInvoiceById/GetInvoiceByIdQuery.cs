using Ardalis.Result;
using MediatR;
using TecnicoApp.Application.Features.Invoices.DTOs;

namespace TecnicoApp.Application.Features.Invoices.Queries.GetInvoiceById;

public record GetInvoiceByIdQuery(Guid Id) : IRequest<Result<InvoiceDto>>;
