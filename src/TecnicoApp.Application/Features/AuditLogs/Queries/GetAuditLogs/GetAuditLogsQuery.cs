using Ardalis.Result;
using MediatR;
using TecnicoApp.Application.Common.DTOs;
using TecnicoApp.Application.Features.AuditLogs.DTOs;

namespace TecnicoApp.Application.Features.AuditLogs.Queries.GetAuditLogs;

public record GetAuditLogsQuery(
    int Page = 1,
    int PageSize = 50,
    string? EntityType = null,
    DateTime? From = null,
    DateTime? To = null
) : IRequest<Result<PaginatedResult<AuditLogDto>>>;
