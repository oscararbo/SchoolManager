using Back.Api.Application.Services;
using Back.Api.Presentation.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back.Api.Presentation.Controllers;

[ApiController]
[Route("api/admin/csv")]
[Authorize(Policy = "AdminOnly")]
public class ImportController(IImportService importService) : ControllerBase
{
    private static async Task<string> ReadTextAsync(IFormFile file)
    {
        using var reader = new StreamReader(file.OpenReadStream());
        return await reader.ReadToEndAsync();
    }

    private static bool EsCsvValido(IFormFile? file)
    {
        return file is not null
            && file.Length > 0
            && string.Equals(Path.GetExtension(file.FileName), ".csv", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Importa cursos a partir de un CSV con cabecera: nombre.
    /// </summary>
    [HttpPost("cursos")]
    public async Task<IActionResult> ImportarCursos(IFormFile file)
    {
        if (!EsCsvValido(file))
            return BadRequest("Sube un archivo CSV valido.");

        return this.ToActionResult(await importService.ImportarCursosAsync(await ReadTextAsync(file), HttpContext.RequestAborted));
    }

    /// <summary>
    /// Importa asignaturas a partir de un CSV con cabecera: nombre,cursoNombre.
    /// </summary>
    [HttpPost("asignaturas")]
    public async Task<IActionResult> ImportarAsignaturas(IFormFile file)
    {
        if (!EsCsvValido(file))
            return BadRequest("Sube un archivo CSV valido.");

        return this.ToActionResult(await importService.ImportarAsignaturasAsync(await ReadTextAsync(file), HttpContext.RequestAborted));
    }

    /// <summary>
    /// Importa profesores a partir de un CSV con cabecera: nombre,correo,contrasena.
    /// </summary>
    [HttpPost("profesores")]
    public async Task<IActionResult> ImportarProfesores(IFormFile file)
    {
        if (!EsCsvValido(file))
            return BadRequest("Sube un archivo CSV valido.");

        return this.ToActionResult(await importService.ImportarProfesoresAsync(await ReadTextAsync(file), HttpContext.RequestAborted));
    }

    /// <summary>
    /// Importa estudiantes a partir de un CSV con cabecera: nombre,correo,contrasena,cursoNombre.
    /// </summary>
    [HttpPost("estudiantes")]
    public async Task<IActionResult> ImportarEstudiantes(IFormFile file)
    {
        if (!EsCsvValido(file))
            return BadRequest("Sube un archivo CSV valido.");

        return this.ToActionResult(await importService.ImportarEstudiantesAsync(await ReadTextAsync(file), HttpContext.RequestAborted));
    }

    /// <summary>
    /// Importa tareas a partir de un CSV con cabecera: profesorCorreo,asignaturaNombre,cursoNombre,trimestre,tareaNombre.
    /// </summary>
    [HttpPost("tareas")]
    public async Task<IActionResult> ImportarTareas(IFormFile file)
    {
        if (!EsCsvValido(file))
            return BadRequest("Sube un archivo CSV valido.");

        return this.ToActionResult(await importService.ImportarTareasAsync(await ReadTextAsync(file), HttpContext.RequestAborted));
    }

    /// <summary>
    /// Importa matriculas a partir de un CSV con cabecera: estudianteCorreo,asignaturaNombre,cursoNombre.
    /// </summary>
    [HttpPost("matriculas")]
    public async Task<IActionResult> ImportarMatriculas(IFormFile file)
    {
        if (!EsCsvValido(file))
            return BadRequest("Sube un archivo CSV valido.");

        return this.ToActionResult(await importService.ImportarMatriculasAsync(await ReadTextAsync(file), HttpContext.RequestAborted));
    }

    /// <summary>
    /// Importa imparticiones a partir de un CSV con cabecera: profesorCorreo,asignaturaNombre,cursoNombre.
    /// </summary>
    [HttpPost("imparticiones")]
    public async Task<IActionResult> ImportarImparticiones(IFormFile file)
    {
        if (!EsCsvValido(file))
            return BadRequest("Sube un archivo CSV valido.");

        return this.ToActionResult(await importService.ImportarImparticionesAsync(await ReadTextAsync(file), HttpContext.RequestAborted));
    }

    /// <summary>
    /// Importa notas a partir de un CSV con cabecera: profesorCorreo,estudianteCorreo,asignaturaNombre,cursoNombre,trimestre,tareaNombre,valor.
    /// </summary>
    [HttpPost("notas")]
    public async Task<IActionResult> ImportarNotas(IFormFile file)
    {
        if (!EsCsvValido(file))
            return BadRequest("Sube un archivo CSV valido.");

        return this.ToActionResult(await importService.ImportarNotasAsync(await ReadTextAsync(file), HttpContext.RequestAborted));
    }
}
