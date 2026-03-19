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

        if (errores.Count > 0)
        {
            return BadRequest(new
            {
                detail = "La importacion de asignaturas ha fallado y se ha cancelado.",
                mensaje = "La importacion de asignaturas ha fallado y se ha cancelado.",
                creados = 0,
                errores
            });
        }

        await context.SaveChangesAsync();
        return Ok(new { creados, errores });
    }

    /// <summary>
    /// Importa profesores a partir de un CSV con cabecera: nombre,correo,contrasena.
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
                // Siempre se hashea la contrasena aunque venga desde CSV.
                Contrasena = passwordService.Hash(contrasena)
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
                // Siempre se hashea la contrasena aunque venga desde CSV.
                Contrasena = passwordService.Hash(contrasena),
                CursoId = curso.Id
            });
            creados++;
        }

        if (errores.Count > 0)
        {
            return BadRequest(new
            {
                detail = "La importacion de estudiantes ha fallado y se ha cancelado.",
                mensaje = "La importacion de estudiantes ha fallado y se ha cancelado.",
                creados = 0,
                errores
            });
        }

        await context.SaveChangesAsync();
        return Ok(new { creados, errores });
    }

    /// <summary>
    /// Importa matriculas a partir de un CSV con cabecera: estudianteCorreo,asignaturaNombre,cursoNombre.
    /// </summary>
    [HttpPost("matriculas")]
    public async Task<IActionResult> ImportarMatriculas(IFormFile file)
    {
        if (!EsCsvValido(file))
            return BadRequest("Sube un archivo CSV valido.");

        var text = await ReadTextAsync(file);
        var estudiantes = await context.Estudiantes.ToListAsync();
        var cursos = await context.Cursos.ToListAsync();
        var asignaturas = await context.Asignaturas.ToListAsync();
        var creados = 0;
        var omitidos = new List<string>();
        var errores = new List<string>();

        foreach (var row in ParseCsv(text))
        {
            if (row.Columns.Length < 3)
            {
                errores.Add($"Linea {row.LineNumber}: se esperaban las columnas estudianteCorreo,asignaturaNombre,cursoNombre.");
                continue;
            }

            var estudianteCorreo = row.Columns[0].Trim().ToLowerInvariant();
            var asignaturaNombre = row.Columns[1].Trim();
            var cursoNombre = row.Columns[2].Trim();

            if (string.IsNullOrWhiteSpace(estudianteCorreo) || string.IsNullOrWhiteSpace(asignaturaNombre) || string.IsNullOrWhiteSpace(cursoNombre))
            {
                errores.Add($"Linea {row.LineNumber}: todos los campos son obligatorios.");
                continue;
            }

            var estudiante = estudiantes.FirstOrDefault(e => e.Correo == estudianteCorreo);
            if (estudiante is null)
            {
                errores.Add($"Linea {row.LineNumber}: estudiante no encontrado '{estudianteCorreo}'.");
                continue;
            }

            var curso = cursos.FirstOrDefault(c => c.Nombre.Equals(cursoNombre, StringComparison.OrdinalIgnoreCase));
            if (curso is null)
            {
                errores.Add($"Linea {row.LineNumber}: curso no encontrado '{cursoNombre}'.");
                continue;
            }

            var asignatura = asignaturas.FirstOrDefault(a =>
                a.CursoId == curso.Id &&
                a.Nombre.Equals(asignaturaNombre, StringComparison.OrdinalIgnoreCase));

            if (asignatura is null)
            {
                errores.Add($"Linea {row.LineNumber}: asignatura no encontrada '{asignaturaNombre}' en '{cursoNombre}'.");
                continue;
            }

            if (estudiante.CursoId != curso.Id)
            {
                errores.Add($"Linea {row.LineNumber}: el estudiante '{estudianteCorreo}' solo puede matricularse en asignaturas de su curso.");
                continue;
            }

            var yaMatriculado = await context.EstudianteAsignaturas
                .AnyAsync(x => x.EstudianteId == estudiante.Id && x.AsignaturaId == asignatura.Id);

            if (yaMatriculado)
            {
                omitidos.Add($"{estudianteCorreo} -> {asignaturaNombre} ({cursoNombre})");
                continue;
            }

            context.EstudianteAsignaturas.Add(new EstudianteAsignatura
            {
                EstudianteId = estudiante.Id,
                AsignaturaId = asignatura.Id
            });
            creados++;
        }

        if (errores.Count > 0)
        {
            return BadRequest(new
            {
                detail = "La importacion de matriculas ha fallado y se ha cancelado.",
                mensaje = "La importacion de matriculas ha fallado y se ha cancelado.",
                creados = 0,
                omitidos = omitidos.Count,
                errores,
                detalles = omitidos
            });
        }

        await context.SaveChangesAsync();
        return Ok(new { creados, omitidos = omitidos.Count, errores, detalles = omitidos });
    }

    /// <summary>
    /// Importa imparticiones a partir de un CSV con cabecera: profesorCorreo,asignaturaNombre,cursoNombre.
    /// </summary>
    [HttpPost("imparticiones")]
    public async Task<IActionResult> ImportarImparticiones(IFormFile file)
    {
        if (!EsCsvValido(file))
            return BadRequest("Sube un archivo CSV valido.");

        var text = await ReadTextAsync(file);
        var profesores = await context.Profesores.ToListAsync();
        var cursos = await context.Cursos.ToListAsync();
        var asignaturas = await context.Asignaturas.ToListAsync();
        var creados = 0;
        var omitidos = new List<string>();
        var errores = new List<string>();

        foreach (var row in ParseCsv(text))
        {
            if (row.Columns.Length < 3)
            {
                errores.Add($"Linea {row.LineNumber}: se esperaban las columnas profesorCorreo,asignaturaNombre,cursoNombre.");
                continue;
            }

            var profesorCorreo = row.Columns[0].Trim().ToLowerInvariant();
            var asignaturaNombre = row.Columns[1].Trim();
            var cursoNombre = row.Columns[2].Trim();

            if (string.IsNullOrWhiteSpace(profesorCorreo) || string.IsNullOrWhiteSpace(asignaturaNombre) || string.IsNullOrWhiteSpace(cursoNombre))
            {
                errores.Add($"Linea {row.LineNumber}: todos los campos son obligatorios.");
                continue;
            }

            var profesor = profesores.FirstOrDefault(p => p.Correo == profesorCorreo);
            if (profesor is null)
            {
                errores.Add($"Linea {row.LineNumber}: profesor no encontrado '{profesorCorreo}'.");
                continue;
            }

            var curso = cursos.FirstOrDefault(c => c.Nombre.Equals(cursoNombre, StringComparison.OrdinalIgnoreCase));
            if (curso is null)
            {
                errores.Add($"Linea {row.LineNumber}: curso no encontrado '{cursoNombre}'.");
                continue;
            }

            var asignatura = asignaturas.FirstOrDefault(a =>
                a.CursoId == curso.Id &&
                a.Nombre.Equals(asignaturaNombre, StringComparison.OrdinalIgnoreCase));

            if (asignatura is null)
            {
                errores.Add($"Linea {row.LineNumber}: asignatura no encontrada '{asignaturaNombre}' en '{cursoNombre}'.");
                continue;
            }

            var asignaturaYaTieneProfesor = await context.ProfesorAsignaturaCursos
                .AnyAsync(x => x.AsignaturaId == asignatura.Id && x.ProfesorId != profesor.Id);

            if (asignaturaYaTieneProfesor)
            {
                errores.Add($"Linea {row.LineNumber}: la asignatura '{asignaturaNombre}' en '{cursoNombre}' ya tiene un profesor asignado.");
                continue;
            }

            var yaAsignado = await context.ProfesorAsignaturaCursos.AnyAsync(x =>
                x.ProfesorId == profesor.Id &&
                x.AsignaturaId == asignatura.Id &&
                x.CursoId == curso.Id);

            if (yaAsignado)
            {
                omitidos.Add($"{profesorCorreo} -> {asignaturaNombre} ({cursoNombre})");
                continue;
            }

            context.ProfesorAsignaturaCursos.Add(new ProfesorAsignaturaCurso
            {
                ProfesorId = profesor.Id,
                AsignaturaId = asignatura.Id,
                CursoId = curso.Id
            });
            creados++;
        }

        if (errores.Count > 0)
        {
            return BadRequest(new
            {
                detail = "La importacion de imparticiones ha fallado y se ha cancelado.",
                mensaje = "La importacion de imparticiones ha fallado y se ha cancelado.",
                creados = 0,
                omitidos = omitidos.Count,
                errores,
                detalles = omitidos
            });
        }

        await context.SaveChangesAsync();
        return Ok(new { creados, omitidos = omitidos.Count, errores, detalles = omitidos });
    }
}
