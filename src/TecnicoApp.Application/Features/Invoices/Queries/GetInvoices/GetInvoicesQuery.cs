using Ardalis.Result;
using MediatR;
using TecnicoApp.Application.Common.DTOs;
using TecnicoApp.Application.Features.Invoices.DTOs;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Invoices.Queries.GetInvoices;

public record GetInvoicesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    InvoiceStatus? Status = null,
    Guid? ClientId = null
) : IRequest<Result<PaginatedResult<InvoiceListItemDto>>>;
