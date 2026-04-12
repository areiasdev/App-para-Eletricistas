using Ardalis.Result;
using MediatR;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Quotes.Commands.UpdateQuoteStatus;

public record UpdateQuoteStatusCommand(Guid Id, QuoteStatus Status) : IRequest<Result>;
