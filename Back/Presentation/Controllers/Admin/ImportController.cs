using Back.Api.Application.Configuration;
using Back.Api.Application.Services;
using Back.Api.Application.Services.Audit;
using Back.Api.Presentation.Http;
using Back.Api.Presentation.Contracts;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back.Api.Presentation.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/admin/csv")]
[Consumes("multipart/form-data")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class ImportController(IImportService importService, IAuditLogService auditLog) : ControllerBase
{
    private async Task<IActionResult> ImportarCsvAsync(
        CsvImportRequest request,
        string entidad,
        Func<string, CancellationToken, Task<Application.Common.ApplicationResult>> importAction)
    {
        var csvText = await ReadTextAsync(request.File!);
        return this.ToActionResult(await importAction(csvText, HttpContext.RequestAborted));
    }

    [HttpPost("export-log/{entidad}")]
    public async Task<IActionResult> RegistrarExportacionExcel(
        [FromRoute] string entidad,
        [FromQuery] int totalRegistros,
        [FromQuery] string? fileName = null)
    {
        await auditLog.LogExcelExportAsync(entidad, totalRegistros, fileName, HttpContext.RequestAborted);
        return NoContent();
    }

    [HttpPost("invalid/{entidad}")]
    public async Task<IActionResult> LogInvalidCsv([FromRoute] string entidad, [FromQuery] string reason)
    {
        await auditLog.LogInvalidCsvAsync(entidad, reason, HttpContext.RequestAborted);
        return NoContent();
    }

    private static async Task<string> ReadTextAsync(IFormFile file)
    {
        using var reader = new StreamReader(file.OpenReadStream());
        return await reader.ReadToEndAsync();
    }

    [HttpPost("cursos")]
    public async Task<IActionResult> ImportarCursos([FromForm] CsvImportRequest request)
        => await ImportarCsvAsync(request, "cursos", importService.ImportarCursosAsync);
    [HttpPost("asignaturas")]
    public async Task<IActionResult> ImportarAsignaturas([FromForm] CsvImportRequest request)
        => await ImportarCsvAsync(request, "asignaturas", importService.ImportarAsignaturasAsync);
    [HttpPost("profesores")]
    public async Task<IActionResult> ImportarProfesores([FromForm] CsvImportRequest request)
        => await ImportarCsvAsync(request, "profesores", importService.ImportarProfesoresAsync);
    [HttpPost("estudiantes")]
    public async Task<IActionResult> ImportarEstudiantes([FromForm] CsvImportRequest request)
        => await ImportarCsvAsync(request, "estudiantes", importService.ImportarEstudiantesAsync);
    [HttpPost("tareas")]
    public async Task<IActionResult> ImportarTareas([FromForm] CsvImportRequest request)
        => await ImportarCsvAsync(request, "tareas", importService.ImportarTareasAsync);
    [HttpPost("horarios")]
    public async Task<IActionResult> ImportarHorarios([FromForm] CsvImportRequest request)
        => await ImportarCsvAsync(request, "horarios", importService.ImportarHorariosAsync);
    [HttpPost("matriculas")]
    public async Task<IActionResult> ImportarMatriculas([FromForm] CsvImportRequest request)
        => await ImportarCsvAsync(request, "matriculas", importService.ImportarMatriculasAsync);
    [HttpPost("imparticiones")]
    public async Task<IActionResult> ImportarImparticiones([FromForm] CsvImportRequest request)
        => await ImportarCsvAsync(request, "imparticiones", importService.ImportarImparticionesAsync);
    [HttpPost("notas")]
    public async Task<IActionResult> ImportarNotas([FromForm] CsvImportRequest request)
        => await ImportarCsvAsync(request, "notas", importService.ImportarNotasAsync);
}

