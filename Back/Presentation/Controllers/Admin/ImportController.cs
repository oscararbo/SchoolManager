using Back.Api.Application.Configuration;
using Back.Api.Application.Services;
using Back.Api.Presentation.Http;
using Back.Api.Presentation.OpenApi;
using Back.Api.Presentation.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back.Api.Presentation.Controllers;

[ApiController]
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

    /// <summary>
    /// Importa cursos a partir de un CSV con cabecera: nombre.
    /// </summary>
    [HttpPost("cursos")]
    [CsvImportExample("nombre\n1 ESO\n2 ESO")]
    public async Task<IActionResult> ImportarCursos([FromForm] CsvImportRequest request)
        => await ImportarCsvAsync(request, importService.ImportarCursosAsync);

    /// <summary>
    /// Importa asignaturas a partir de un CSV con cabecera: nombre,cursoNombre.
    /// </summary>
    [HttpPost("asignaturas")]
    [CsvImportExample("nombre,cursoNombre\nMatematicas,1 ESO\nLengua,1 ESO")]
    public async Task<IActionResult> ImportarAsignaturas([FromForm] CsvImportRequest request)
        => await ImportarCsvAsync(request, importService.ImportarAsignaturasAsync);

    /// <summary>
    /// Importa profesores a partir de un CSV con cabecera: nombre,correo,contrasena.
    /// </summary>
    [HttpPost("profesores")]
    [CsvImportExample("nombre,correo,contrasena\nAna Lopez,ana@centro.com,Clave123")]
    public async Task<IActionResult> ImportarProfesores([FromForm] CsvImportRequest request)
        => await ImportarCsvAsync(request, importService.ImportarProfesoresAsync);

    /// <summary>
    /// Importa estudiantes a partir de un CSV con cabecera: nombre,correo,contrasena,cursoNombre.
    /// </summary>
    [HttpPost("estudiantes")]
    [CsvImportExample("nombre,correo,contrasena,cursoNombre\nLuis Perez,luis@centro.com,Clave123,1 ESO")]
    public async Task<IActionResult> ImportarEstudiantes([FromForm] CsvImportRequest request)
        => await ImportarCsvAsync(request, importService.ImportarEstudiantesAsync);

    /// <summary>
    /// Importa tareas a partir de un CSV con cabecera: profesorCorreo,asignaturaNombre,cursoNombre,trimestre,tareaNombre.
    /// </summary>
    [HttpPost("tareas")]
    [CsvImportExample("profesorCorreo,asignaturaNombre,cursoNombre,trimestre,tareaNombre\nana@centro.com,Matematicas,1 ESO,1,Examen T1")]
    public async Task<IActionResult> ImportarTareas([FromForm] CsvImportRequest request)
        => await ImportarCsvAsync(request, importService.ImportarTareasAsync);

    /// <summary>
    /// Importa matriculas a partir de un CSV con cabecera: estudianteCorreo,asignaturaNombre,cursoNombre.
    /// </summary>
    [HttpPost("matriculas")]
    [CsvImportExample("estudianteCorreo,asignaturaNombre,cursoNombre\nluis@centro.com,Matematicas,1 ESO")]
    public async Task<IActionResult> ImportarMatriculas([FromForm] CsvImportRequest request)
        => await ImportarCsvAsync(request, importService.ImportarMatriculasAsync);

    /// <summary>
    /// Importa imparticiones a partir de un CSV con cabecera: profesorCorreo,asignaturaNombre,cursoNombre.
    /// </summary>
    [HttpPost("imparticiones")]
    [CsvImportExample("profesorCorreo,asignaturaNombre,cursoNombre\nana@centro.com,Matematicas,1 ESO")]
    public async Task<IActionResult> ImportarImparticiones([FromForm] CsvImportRequest request)
        => await ImportarCsvAsync(request, importService.ImportarImparticionesAsync);

    /// <summary>
    /// Importa notas a partir de un CSV con cabecera: profesorCorreo,estudianteCorreo,asignaturaNombre,cursoNombre,trimestre,tareaNombre,valor.
    /// </summary>
    [HttpPost("notas")]
    [CsvImportExample("profesorCorreo,estudianteCorreo,asignaturaNombre,cursoNombre,trimestre,tareaNombre,valor\nana@centro.com,luis@centro.com,Matematicas,1 ESO,1,Examen T1,7.5")]
    public async Task<IActionResult> ImportarNotas([FromForm] CsvImportRequest request)
        => await ImportarCsvAsync(request, importService.ImportarNotasAsync);
}
