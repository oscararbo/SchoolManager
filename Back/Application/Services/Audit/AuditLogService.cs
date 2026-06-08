using Serilog.Context;
using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Dtos;
using Back.Api.Application.Common;

namespace Back.Api.Application.Services.Audit;

public class AuditLogService(ILogger<AuditLogService> logger, IAuditLogDomainRepository repository) : IAuditLogService
{
    private static string? ExtractEventType(string? message)
    {
        if (string.IsNullOrWhiteSpace(message)) return null;

        var start = message.IndexOf("[Audit:");
        if (start == -1) return null;

        var end = message.IndexOf("]", start);
        if (end == -1) return null;

        return message.Substring(start + 7, end - start - 7);
    }

    private static void EnrichLogs(IEnumerable<AdminLogDto> logs)
    {
        foreach (var log in logs)
        {
            log.EventType = ExtractEventType(log.Message);
        }
    }

    public async Task<ApplicationResult> SearchAsync(LogsQueryRequest request, CancellationToken ct = default)
    {
        var result = await repository.SearchAsync(request, ct);

        EnrichLogs(result.Items);

        return ApplicationResult.Ok(result);
    }

    public async Task<ApplicationResult> GetTimelineAsync(LogsQueryRequest request, CancellationToken ct = default)
    {
        var result = await repository.GetTimelineAsync(request, ct);

        return ApplicationResult.Ok(result);
    }

    public Task LogLoginAttemptAsync(string? correo, bool succeeded, string? colegioSlug = null, CancellationToken cancellationToken = default)
    {
        var eventType = succeeded ? "LoginSuccess" : "LoginFailure";

        using var _1 = LogContext.PushProperty("UserEmail", correo ?? "anonymous");
        using var _2 = LogContext.PushProperty("Succeeded", succeeded);
        using var _3 = LogContext.PushProperty("ColegioSlug", colegioSlug);

        logger.LogInformation(
            "[Audit:{EventType}] Intento de inicio de sesión para {UserEmail}. Success={Succeeded}",
            eventType, correo ?? "anonymous", succeeded
        );

        return Task.CompletedTask;
    }

    public Task LogLogoutAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[Audit:{EventType}] Cerrar sesión de usuario.", "Logout");
        return Task.CompletedTask;
    }

    public Task LogTokenRefreshAsync(bool succeeded, string? correo = null, CancellationToken cancellationToken = default)
    {
        var eventType = succeeded ? "TokenRefresh" : "TokenRefreshFailure";

        using var _1 = correo is not null ? LogContext.PushProperty("UserEmail", correo) : null;
        using var _2 = LogContext.PushProperty("Succeeded", succeeded);

        logger.LogInformation(
            "[Audit:{EventType}] Actualización de token para {UserEmail}. Success={Succeeded}",
            eventType, correo ?? "anonymous", succeeded
        );

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

        logger.LogInformation(
            "[Audit:{EventType}] Importación de CSV para {Entity}. Success={Succeeded}, Creados={Creados}, Omitidos={Omitidos}, Errores={Errores}, Details={Details}",
            "CsvImport", entity, succeeded, creados, omitidos, errores, details
        );

        return Task.CompletedTask;
    }

    public Task LogExcelExportAsync(string entity, int totalRegistros, string? fileName = null, CancellationToken cancellationToken = default)
    {
        using var _1 = LogContext.PushProperty("Entity", entity);
        using var _2 = LogContext.PushProperty("TotalRegistros", totalRegistros);
        using var _3 = fileName is not null ? LogContext.PushProperty("FileName", fileName) : null;

        logger.LogInformation(
            "[Audit:{EventType}] Exportación de Excel para {Entity}. TotalRegistros={TotalRegistros}, FileName={FileName}",
            "ExcelExport", entity, totalRegistros, fileName
        );

        return Task.CompletedTask;
    }

    public Task LogInvalidCsvAsync(string entity, string reason, CancellationToken cancellationToken = default)
    {
        using var _1 = LogContext.PushProperty("Entity", entity);
        using var _2 = LogContext.PushProperty("Reason", reason);

        logger.LogWarning(
            "[Audit:{EventType}] CSV inválido para {Entity}. Reason={Reason}",
            "InvalidCsv", entity, reason
        );

        return Task.CompletedTask;
    }
}