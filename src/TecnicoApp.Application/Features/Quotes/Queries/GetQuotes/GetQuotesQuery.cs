using Ardalis.Result;
using MediatR;
using TecnicoApp.Application.Common.DTOs;
using TecnicoApp.Application.Features.Quotes.DTOs;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Quotes.Queries.GetQuotes;

public record GetQuotesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    QuoteStatus? Status = null,
    Guid? ClientId = null
) : IRequest<Result<PaginatedResult<QuoteListItemDto>>>;
