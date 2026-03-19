using Back.Api.Data;
using Back.Api.Models;
using Back.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Back.Api.Controllers;

[ApiController]
[Route("api/admin/csv")]
[Authorize(Policy = "AdminOnly")]
public class ImportController(AppDbContext context, IPasswordService passwordService) : ControllerBase
{
    private sealed record CsvRow(int LineNumber, string[] Columns);

    /// <summary>
    /// Convierte el contenido del CSV en filas normalizadas y conserva el numero de linea original.
    /// </summary>
    private static IEnumerable<CsvRow> ParseCsv(string text)
    {
        var lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        var first = true;
        var lineNumber = 0;

        foreach (var raw in lines)
        {
            lineNumber++;
            var line = raw.Trim();
            if (string.IsNullOrEmpty(line)) continue;
            if (first)
            {
                first = false;
                continue;
            }

            yield return new CsvRow(lineNumber, line.Split(',').Select(c => c.Trim().Trim('"')).ToArray());
        }
    }

    /// <summary>
    /// Lee el contenido completo del archivo subido por formulario.
    /// </summary>
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

        var text = await ReadTextAsync(file);
        var creados = new List<string>();
        var omitidos = new List<string>();
        var errores = new List<string>();

        foreach (var row in ParseCsv(text))
        {
            if (row.Columns.Length < 1 || string.IsNullOrWhiteSpace(row.Columns[0]))
            {
                errores.Add($"Linea {row.LineNumber}: el nombre del curso es obligatorio.");
                continue;
            }

            var nombre = row.Columns[0].Trim();
            var existe = await context.Cursos.AnyAsync(c => c.Nombre == nombre);
            if (existe) { omitidos.Add(nombre); continue; }
            context.Cursos.Add(new Curso { Nombre = nombre });
            creados.Add(nombre);
        }

        await context.SaveChangesAsync();
        return Ok(new { creados = creados.Count, omitidos = omitidos.Count, errores, detalles = omitidos });
    }

    /// <summary>
    /// Importa asignaturas a partir de un CSV con cabecera: nombre,cursoNombre.
    /// </summary>
    [HttpPost("asignaturas")]
    public async Task<IActionResult> ImportarAsignaturas(IFormFile file)
    {
        if (!EsCsvValido(file))
            return BadRequest("Sube un archivo CSV valido.");

        var text = await ReadTextAsync(file);
        var cursos = await context.Cursos.ToListAsync();
        var creados = 0;
        var errores = new List<string>();

        foreach (var row in ParseCsv(text))
        {
            if (row.Columns.Length < 2)
            {
                errores.Add($"Linea {row.LineNumber}: se esperaban las columnas nombre,cursoNombre.");
                continue;
            }

            var nombre = row.Columns[0].Trim();
            var cursoNombre = row.Columns[1].Trim();
            var curso = cursos.FirstOrDefault(c => c.Nombre.Equals(cursoNombre, StringComparison.OrdinalIgnoreCase));
            if (curso is null) { errores.Add($"Linea {row.LineNumber}: curso no encontrado '{cursoNombre}'."); continue; }
            var existe = await context.Asignaturas.AnyAsync(a => a.CursoId == curso.Id && a.Nombre == nombre);
            if (existe) { errores.Add($"Linea {row.LineNumber}: ya existe '{nombre}' en '{cursoNombre}'."); continue; }
            context.Asignaturas.Add(new Asignatura { Nombre = nombre, CursoId = curso.Id });
            creados++;
        }

        await context.SaveChangesAsync();
        return Ok(new { creados, errores });
    }

    /// <summary>
    /// Importa profesores a partir de un CSV con cabecera: nombre,correo,contrasena,esAdmin.
    /// </summary>
    [HttpPost("profesores")]
    public async Task<IActionResult> ImportarProfesores(IFormFile file)
    {
        if (!EsCsvValido(file))
            return BadRequest("Sube un archivo CSV valido.");

        var text = await ReadTextAsync(file);
        var creados = 0;
        var errores = new List<string>();

        foreach (var row in ParseCsv(text))
        {
            if (row.Columns.Length < 3)
            {
                errores.Add($"Linea {row.LineNumber}: se esperaban al menos nombre,correo,contrasena.");
                continue;
            }

            var nombre = row.Columns[0].Trim();
            var correo = row.Columns[1].Trim().ToLowerInvariant();
            var contrasena = row.Columns[2].Trim();
            var esAdmin = row.Columns.Length > 3 && row.Columns[3].Trim().Equals("true", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(contrasena))
            {
                errores.Add($"Linea {row.LineNumber}: datos incompletos para '{nombre}'."); continue;
            }

            var existe = await context.Profesores.AnyAsync(p => p.Correo == correo);
            if (existe) { errores.Add($"Linea {row.LineNumber}: correo duplicado {correo}."); continue; }

            context.Profesores.Add(new Profesor
            {
                Nombre = nombre,
                Correo = correo,
                Contrasena = passwordService.Hash(contrasena),
                EsAdmin = esAdmin
            });
            creados++;
        }

        await context.SaveChangesAsync();
        return Ok(new { creados, errores });
    }

    /// <summary>
    /// Importa estudiantes a partir de un CSV con cabecera: nombre,correo,contrasena,cursoNombre.
    /// </summary>
    [HttpPost("estudiantes")]
    public async Task<IActionResult> ImportarEstudiantes(IFormFile file)
    {
        if (!EsCsvValido(file))
            return BadRequest("Sube un archivo CSV valido.");

        var text = await ReadTextAsync(file);
        var cursos = await context.Cursos.ToListAsync();
        var creados = 0;
        var errores = new List<string>();

        foreach (var row in ParseCsv(text))
        {
            if (row.Columns.Length < 4)
            {
                errores.Add($"Linea {row.LineNumber}: se esperaban las columnas nombre,correo,contrasena,cursoNombre.");
                continue;
            }

            var nombre = row.Columns[0].Trim();
            var correo = row.Columns[1].Trim().ToLowerInvariant();
            var contrasena = row.Columns[2].Trim();
            var cursoNombre = row.Columns[3].Trim();

            if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(contrasena))
            {
                errores.Add($"Linea {row.LineNumber}: datos incompletos para '{nombre}'."); continue;
            }

            var curso = cursos.FirstOrDefault(c => c.Nombre.Equals(cursoNombre, StringComparison.OrdinalIgnoreCase));
            if (curso is null) { errores.Add($"Linea {row.LineNumber}: curso no encontrado '{cursoNombre}'."); continue; }

            var existe = await context.Estudiantes.AnyAsync(e => e.Correo == correo);
            if (existe) { errores.Add($"Linea {row.LineNumber}: correo duplicado {correo}."); continue; }

            context.Estudiantes.Add(new Estudiante
            {
                Nombre = nombre,
                Correo = correo,
                Contrasena = passwordService.Hash(contrasena),
                CursoId = curso.Id
            });
            creados++;
        }

        await context.SaveChangesAsync();
        return Ok(new { creados, errores });
    }
}
