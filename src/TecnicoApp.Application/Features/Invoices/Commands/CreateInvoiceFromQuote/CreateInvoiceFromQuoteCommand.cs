using Ardalis.Result;
using MediatR;
using TecnicoApp.Application.Features.Invoices.DTOs;

namespace TecnicoApp.Application.Features.Invoices.Commands.CreateInvoiceFromQuote;

public record CreateInvoiceFromQuoteCommand(Guid QuoteId) : IRequest<Result<InvoiceDto>>;
