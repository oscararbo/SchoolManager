using Back.Api.Application.Configuration;
using Back.Api.Application.Services;
using Back.Api.Presentation.Http;
using Back.Api.Presentation.Requests;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back.Api.Presentation.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/admin/csv")]
[Consumes("multipart/form-data")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class ImportController(IImportService importService) : ControllerBase
{
    private async Task<IActionResult> ImportarCsvAsync(
        CsvImportRequest request,
        Func<string, CancellationToken, Task<Back.Api.Application.Common.ApplicationResult>> importAction)
    {
        var csvText = await ReadTextAsync(request.File!);
        return this.ToActionResult(await importAction(csvText, HttpContext.RequestAborted));
    }

    private static async Task<string> ReadTextAsync(IFormFile file)
    {
        using var reader = new StreamReader(file.OpenReadStream());
        return await reader.ReadToEndAsync();
    }
    [HttpPost("cursos")]
    public async Task<IActionResult> ImportarCursos([FromForm] CsvImportRequest request)
        => await ImportarCsvAsync(request, importService.ImportarCursosAsync);
    [HttpPost("asignaturas")]
    public async Task<IActionResult> ImportarAsignaturas([FromForm] CsvImportRequest request)
        => await ImportarCsvAsync(request, importService.ImportarAsignaturasAsync);
    [HttpPost("profesores")]
    public async Task<IActionResult> ImportarProfesores([FromForm] CsvImportRequest request)
        => await ImportarCsvAsync(request, importService.ImportarProfesoresAsync);
    [HttpPost("estudiantes")]
    public async Task<IActionResult> ImportarEstudiantes([FromForm] CsvImportRequest request)
        => await ImportarCsvAsync(request, importService.ImportarEstudiantesAsync);
    [HttpPost("tareas")]
    public async Task<IActionResult> ImportarTareas([FromForm] CsvImportRequest request)
        => await ImportarCsvAsync(request, importService.ImportarTareasAsync);
    [HttpPost("matriculas")]
    public async Task<IActionResult> ImportarMatriculas([FromForm] CsvImportRequest request)
        => await ImportarCsvAsync(request, importService.ImportarMatriculasAsync);
    [HttpPost("imparticiones")]
    public async Task<IActionResult> ImportarImparticiones([FromForm] CsvImportRequest request)
        => await ImportarCsvAsync(request, importService.ImportarImparticionesAsync);
    [HttpPost("notas")]
    public async Task<IActionResult> ImportarNotas([FromForm] CsvImportRequest request)
        => await ImportarCsvAsync(request, importService.ImportarNotasAsync);
}


