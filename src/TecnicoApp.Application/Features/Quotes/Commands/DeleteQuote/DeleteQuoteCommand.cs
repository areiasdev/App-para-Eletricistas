using Ardalis.Result;
using MediatR;

namespace TecnicoApp.Application.Features.Quotes.Commands.DeleteQuote;

public record DeleteQuoteCommand(Guid Id) : IRequest<Result>;
