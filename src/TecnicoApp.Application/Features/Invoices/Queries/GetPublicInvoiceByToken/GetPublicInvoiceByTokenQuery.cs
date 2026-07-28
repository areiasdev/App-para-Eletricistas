using Ardalis.Result;
using MediatR;
using TecnicoApp.Application.Features.Invoices.DTOs;

namespace TecnicoApp.Application.Features.Invoices.Queries.GetPublicInvoiceByToken;

// No auth, no ownerId resolution — this is the whole point: an anonymous customer reaches this
// only via a magic-link token, never a logged-in session.
public record GetPublicInvoiceByTokenQuery(string Token) : IRequest<Result<PublicInvoiceDto>>;
