namespace Back.Api.Application.Services.Audit;

public interface IAuditLogService
{
    Task LogLoginAttemptAsync(string? correo, bool succeeded, string? colegioSlug = null, CancellationToken cancellationToken = default);
    Task LogLogoutAsync(CancellationToken cancellationToken = default);
    Task LogTokenRefreshAsync(bool succeeded, string? correo = null, CancellationToken cancellationToken = default);
    Task LogCsvImportAsync(string entity, bool succeeded, int creados, int omitidos, int errores, string? details = null, CancellationToken cancellationToken = default);
    Task LogExcelExportAsync(string entity, int totalRegistros, string? fileName = null, CancellationToken cancellationToken = default);
}
