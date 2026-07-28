using Ardalis.Result;
using MediatR;

namespace TecnicoApp.Application.Features.Invoices.Commands.CreateInvoiceCheckout;

// Public, unauthenticated — same token validation as GetPublicInvoiceByTokenQuery.
public record CreateInvoiceCheckoutCommand(string Token) : IRequest<Result<string>>;
