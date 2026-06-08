using Back.Api.Application.Dtos;
using Back.Api.Application.Common;

namespace Back.Api.Application.Services.Audit;

public interface IAuditLogService
{
    Task LogLoginAttemptAsync(string? correo, bool succeeded, string? colegioSlug = null, CancellationToken cancellationToken = default);
    Task LogLogoutAsync(CancellationToken cancellationToken = default);
    Task LogTokenRefreshAsync(bool succeeded, string? correo = null, CancellationToken cancellationToken = default);
    Task LogCsvImportAsync(string entity, bool succeeded, int creados, int omitidos, int errores, string? details = null, CancellationToken cancellationToken = default);
    Task LogExcelExportAsync(string entity, int totalRegistros, string? fileName = null, CancellationToken cancellationToken = default);
    Task LogInvalidCsvAsync(string entity, string reason, CancellationToken cancellationToken = default);
    Task<ApplicationResult> SearchAsync(LogsQueryRequest request, CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetTimelineAsync(LogsQueryRequest request, CancellationToken cancellationToken = default);
}