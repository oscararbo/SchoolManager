using Back.Api.Application.Dtos;

namespace Back.Api.Application.Abstractions.Repositories;

public interface IAuditLogDomainRepository
{
    Task<PaginatedLogsDto> SearchAsync(LogsQueryRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<LogsTimelineDto>> GetTimelineAsync(LogsQueryRequest request, CancellationToken cancellationToken = default);
}
