using Ardalis.Result;
using MediatR;

namespace TecnicoApp.Application.Features.Quotes.Queries.GenerateQuotePdf;

public record QuotePdfResult(byte[] Bytes, string Number);

public record GenerateQuotePdfQuery(Guid QuoteId) : IRequest<Result<QuotePdfResult>>;
