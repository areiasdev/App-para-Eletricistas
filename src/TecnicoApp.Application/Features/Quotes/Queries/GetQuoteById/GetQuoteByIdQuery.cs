using Ardalis.Result;
using MediatR;
using TecnicoApp.Application.Features.Quotes.DTOs;

namespace TecnicoApp.Application.Features.Quotes.Queries.GetQuoteById;

public record GetQuoteByIdQuery(Guid Id) : IRequest<Result<QuoteDto>>;
