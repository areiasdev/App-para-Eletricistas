using Ardalis.Result;
using MediatR;

namespace TecnicoApp.Application.Features.Invoices.Queries.GenerateInvoicePdf;

public record InvoicePdfResult(byte[] Bytes, string Number);

public record GenerateInvoicePdfQuery(Guid InvoiceId) : IRequest<Result<InvoicePdfResult>>;
