using Ardalis.Result;
using MediatR;

namespace TecnicoApp.Application.Features.Quotes.Commands.SendQuoteEmail;

public record SendQuoteEmailCommand(Guid QuoteId) : IRequest<Result>;
