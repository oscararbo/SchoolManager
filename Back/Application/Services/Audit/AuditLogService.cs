using Serilog.Context;

namespace Back.Api.Application.Services.Audit;

public class AuditLogService(ILogger<AuditLogService> logger) : IAuditLogService
{
    public Task LogLoginAttemptAsync(string? correo, bool succeeded, string? colegioSlug = null, CancellationToken cancellationToken = default)
    {
        var eventType = succeeded ? "LoginSuccess" : "LoginFailure";
        using var _1 = LogContext.PushProperty("UserEmail", correo ?? "anonymous");
        using var _2 = LogContext.PushProperty("Succeeded", succeeded);
        using var _3 = LogContext.PushProperty("ColegioSlug", colegioSlug);
        logger.LogInformation("[Audit:{EventType}]", eventType);
        return Task.CompletedTask;
    }

    public Task LogLogoutAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[Audit:{EventType}]", "Logout");
        return Task.CompletedTask;
    }

    public Task LogTokenRefreshAsync(bool succeeded, string? correo = null, CancellationToken cancellationToken = default)
    {
        var eventType = succeeded ? "TokenRefresh" : "TokenRefreshFailure";
        using var _1 = correo is not null ? LogContext.PushProperty("UserEmail", correo) : null;
        using var _2 = LogContext.PushProperty("Succeeded", succeeded);
        logger.LogInformation("[Audit:{EventType}]", eventType);
        return Task.CompletedTask;
    }

    public Task LogCsvImportAsync(string entity, bool succeeded, int creados, int omitidos, int errores, string? details = null, CancellationToken cancellationToken = default)
    {
        using var _1 = LogContext.PushProperty("Entity", entity);
        using var _2 = LogContext.PushProperty("Succeeded", succeeded);
        using var _3 = LogContext.PushProperty("Creados", creados);
        using var _4 = LogContext.PushProperty("Omitidos", omitidos);
        using var _5 = LogContext.PushProperty("Errores", errores);
        using var _6 = details is not null ? LogContext.PushProperty("Details", details) : null;
        logger.LogInformation("[Audit:{EventType}]", "CsvImport");
        return Task.CompletedTask;
    }

    public Task LogExcelExportAsync(string entity, int totalRegistros, string? fileName = null, CancellationToken cancellationToken = default)
    {
        using var _1 = LogContext.PushProperty("Entity", entity);
        using var _2 = LogContext.PushProperty("TotalRegistros", totalRegistros);
        using var _3 = fileName is not null ? LogContext.PushProperty("FileName", fileName) : null;
        logger.LogInformation("[Audit:{EventType}]", "CsvExport");
        return Task.CompletedTask;
    }
}
