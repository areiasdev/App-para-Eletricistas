using Ardalis.Result;
using MediatR;

namespace TecnicoApp.Application.Features.Quotes.Commands.SignQuote;

/// <summary>
/// Signs a quote. SignatureDataUrl must be a base64 PNG data URI
/// (e.g. "data:image/png;base64,iVBORw0KGgo...").
/// </summary>
public record SignQuoteCommand(Guid QuoteId, string SignatureDataUrl) : IRequest<Result>;
