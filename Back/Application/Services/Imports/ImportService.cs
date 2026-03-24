using Back.Api.Application.Common;
using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Abstractions.Security;
using Back.Api.Application.Dtos;

namespace Back.Api.Application.Services;

public class ImportService(IImportRepository importRepository, IPasswordService passwordService) : IImportService
{
    private sealed record CsvRow(int LineNumber, string[] Columns);

    public async Task<ApplicationResult> ImportarCursosAsync(string csvText, CancellationToken cancellationToken = default)
    {
        var creados = new List<string>();
        var omitidos = new List<string>();
        var errores = new List<string>();
        var existentes = (await importRepository.GetCursosAsync(cancellationToken))
            .Select(c => c.Nombre)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var row in ParseCsv(csvText))
        {
            if (row.Columns.Length < 1 || string.IsNullOrWhiteSpace(row.Columns[0]))
            {
                errores.Add($"Linea {row.LineNumber}: el nombre del curso es obligatorio.");
                continue;
            }

            var nombre = row.Columns[0].Trim();
            if (!existentes.Add(nombre))
            {
                omitidos.Add(nombre);
                continue;
            }

            creados.Add(nombre);
        }

        if (creados.Count > 0)
        {
            await importRepository.AddCursosAsync(creados, cancellationToken);
        }

        return ApplicationResult.Ok(new CsvImportResultDto
        {
            Creados = creados.Count,
            Omitidos = omitidos.Count,
            Errores = errores,
            Detalles = omitidos
        });
    }

    public async Task<ApplicationResult> ImportarAsignaturasAsync(string csvText, CancellationToken cancellationToken = default)
    {
        var creados = new List<(string Nombre, int CursoId)>();
        var errores = new List<string>();
        var cursos = (await importRepository.GetCursosAsync(cancellationToken))
            .ToDictionary(c => c.Nombre, c => c, StringComparer.OrdinalIgnoreCase);
        var clavesExistentes = (await importRepository.GetAsignaturasAsync(cancellationToken))
            .Select(a => ToAsignaturaKey(a.CursoId, a.Nombre))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var row in ParseCsv(csvText))
        {
            if (row.Columns.Length < 2)
            {
                errores.Add($"Linea {row.LineNumber}: se esperaban las columnas nombre,cursoNombre.");
                continue;
            }

            var nombre = row.Columns[0].Trim();
            var cursoNombre = row.Columns[1].Trim();
            if (!cursos.TryGetValue(cursoNombre, out var curso))
            {
                errores.Add($"Linea {row.LineNumber}: curso no encontrado '{cursoNombre}'.");
                continue;
            }

            var key = ToAsignaturaKey(curso.Id, nombre);
            if (!clavesExistentes.Add(key))
            {
                errores.Add($"Linea {row.LineNumber}: ya existe '{nombre}' en '{cursoNombre}'.");
                continue;
            }

            creados.Add((nombre, curso.Id));
        }

        if (errores.Count > 0)
        {
            return ApplicationResult.BadRequest(new CsvImportResultDto
            {
                Detail = "La importacion de asignaturas ha fallado y se ha cancelado.",
                Mensaje = "La importacion de asignaturas ha fallado y se ha cancelado.",
                Creados = 0,
                Errores = errores
            });
        }

        if (creados.Count > 0)
        {
            await importRepository.AddAsignaturasAsync(creados, cancellationToken);
        }

        return ApplicationResult.Ok(new CsvImportResultDto
        {
            Creados = creados.Count,
            Errores = errores
        });
    }

    public async Task<ApplicationResult> ImportarProfesoresAsync(string csvText, CancellationToken cancellationToken = default)
    {
        var creados = new List<(string Nombre, string Correo, string ContrasenaHash)>();
        var errores = new List<string>();
        var correos = (await importRepository.GetProfesoresAsync(cancellationToken))
            .Select(p => p.Correo)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var row in ParseCsv(csvText))
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
                errores.Add($"Linea {row.LineNumber}: datos incompletos para '{nombre}'.");
                continue;
            }

            if (!correos.Add(correo))
            {
                errores.Add($"Linea {row.LineNumber}: correo duplicado {correo}.");
                continue;
            }

            creados.Add((nombre, correo, passwordService.Hash(contrasena)));
        }

        if (creados.Count > 0)
        {
            await importRepository.AddProfesoresAsync(creados, cancellationToken);
        }

        return ApplicationResult.Ok(new CsvImportResultDto
        {
            Creados = creados.Count,
            Errores = errores
        });
    }

    public async Task<ApplicationResult> ImportarEstudiantesAsync(string csvText, CancellationToken cancellationToken = default)
    {
        var creados = new List<(string Nombre, string Correo, string ContrasenaHash, int CursoId)>();
        var errores = new List<string>();
        var cursos = (await importRepository.GetCursosAsync(cancellationToken))
            .ToDictionary(c => c.Nombre, c => c, StringComparer.OrdinalIgnoreCase);
        var correos = (await importRepository.GetEstudiantesAsync(cancellationToken))
            .Select(e => e.Correo)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var row in ParseCsv(csvText))
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
                errores.Add($"Linea {row.LineNumber}: datos incompletos para '{nombre}'.");
                continue;
            }

            if (!cursos.TryGetValue(cursoNombre, out var curso))
            {
                errores.Add($"Linea {row.LineNumber}: curso no encontrado '{cursoNombre}'.");
                continue;
            }

            if (!correos.Add(correo))
            {
                errores.Add($"Linea {row.LineNumber}: correo duplicado {correo}.");
                continue;
            }

            creados.Add((nombre, correo, passwordService.Hash(contrasena), curso.Id));
        }

        if (errores.Count > 0)
        {
            return ApplicationResult.BadRequest(new CsvImportResultDto
            {
                Detail = "La importacion de estudiantes ha fallado y se ha cancelado.",
                Mensaje = "La importacion de estudiantes ha fallado y se ha cancelado.",
                Creados = 0,
                Errores = errores
            });
        }

        if (creados.Count > 0)
        {
            await importRepository.AddEstudiantesAsync(creados, cancellationToken);
        }

        return ApplicationResult.Ok(new CsvImportResultDto
        {
            Creados = creados.Count,
            Errores = errores
        });
    }

    public async Task<ApplicationResult> ImportarMatriculasAsync(string csvText, CancellationToken cancellationToken = default)
    {
        var errores = new List<string>();
        var omitidos = new List<string>();
        var creados = new List<(int EstudianteId, int AsignaturaId)>();

        var estudiantes = (await importRepository.GetEstudiantesAsync(cancellationToken))
            .ToDictionary(e => e.Correo, e => e, StringComparer.OrdinalIgnoreCase);
        var cursos = (await importRepository.GetCursosAsync(cancellationToken))
            .ToDictionary(c => c.Nombre, c => c, StringComparer.OrdinalIgnoreCase);
        var asignaturas = await importRepository.GetAsignaturasAsync(cancellationToken);
        var matriculas = (await importRepository.GetMatriculasAsync(cancellationToken)).ToHashSet();

        foreach (var row in ParseCsv(csvText))
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

            if (!estudiantes.TryGetValue(estudianteCorreo, out var estudiante))
            {
                errores.Add($"Linea {row.LineNumber}: estudiante no encontrado '{estudianteCorreo}'.");
                continue;
            }

            if (!cursos.TryGetValue(cursoNombre, out var curso))
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

            var matricula = (estudiante.Id, asignatura.Id);
            if (!matriculas.Add(matricula))
            {
                omitidos.Add($"{estudianteCorreo} -> {asignaturaNombre} ({cursoNombre})");
                continue;
            }

            creados.Add(matricula);
        }

        if (errores.Count > 0)
        {
            return ApplicationResult.BadRequest(new CsvImportResultDto
            {
                Detail = "La importacion de matriculas ha fallado y se ha cancelado.",
                Mensaje = "La importacion de matriculas ha fallado y se ha cancelado.",
                Creados = 0,
                Omitidos = omitidos.Count,
                Errores = errores,
                Detalles = omitidos
            });
        }

        if (creados.Count > 0)
        {
            await importRepository.AddMatriculasAsync(creados, cancellationToken);
        }

        return ApplicationResult.Ok(new CsvImportResultDto
        {
            Creados = creados.Count,
            Omitidos = omitidos.Count,
            Errores = errores,
            Detalles = omitidos
        });
    }

    public async Task<ApplicationResult> ImportarImparticionesAsync(string csvText, CancellationToken cancellationToken = default)
    {
        var errores = new List<string>();
        var omitidos = new List<string>();
        var creados = new List<(int ProfesorId, int AsignaturaId, int CursoId)>();

        var profesores = (await importRepository.GetProfesoresAsync(cancellationToken))
            .ToDictionary(p => p.Correo, p => p, StringComparer.OrdinalIgnoreCase);
        var cursos = (await importRepository.GetCursosAsync(cancellationToken))
            .ToDictionary(c => c.Nombre, c => c, StringComparer.OrdinalIgnoreCase);
        var asignaturas = await importRepository.GetAsignaturasAsync(cancellationToken);
        var imparticiones = await importRepository.GetImparticionesAsync(cancellationToken);
        var asignaturaProfesor = imparticiones
            .GroupBy(x => x.AsignaturaId)
            .ToDictionary(g => g.Key, g => g.First().ProfesorId);
        var combinaciones = imparticiones
            .Select(x => (x.ProfesorId, x.AsignaturaId, x.CursoId))
            .ToHashSet();

        foreach (var row in ParseCsv(csvText))
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

            if (!profesores.TryGetValue(profesorCorreo, out var profesor))
            {
                errores.Add($"Linea {row.LineNumber}: profesor no encontrado '{profesorCorreo}'.");
                continue;
            }

            if (!cursos.TryGetValue(cursoNombre, out var curso))
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

            if (asignaturaProfesor.TryGetValue(asignatura.Id, out var profesorActual) && profesorActual != profesor.Id)
            {
                errores.Add($"Linea {row.LineNumber}: la asignatura '{asignaturaNombre}' en '{cursoNombre}' ya tiene un profesor asignado.");
                continue;
            }

            var imparticion = (profesor.Id, asignatura.Id, curso.Id);
            if (!combinaciones.Add(imparticion))
            {
                omitidos.Add($"{profesorCorreo} -> {asignaturaNombre} ({cursoNombre})");
                continue;
            }

            asignaturaProfesor[asignatura.Id] = profesor.Id;
            creados.Add(imparticion);
        }

        if (errores.Count > 0)
        {
            return ApplicationResult.BadRequest(new CsvImportResultDto
            {
                Detail = "La importacion de imparticiones ha fallado y se ha cancelado.",
                Mensaje = "La importacion de imparticiones ha fallado y se ha cancelado.",
                Creados = 0,
                Omitidos = omitidos.Count,
                Errores = errores,
                Detalles = omitidos
            });
        }

        if (creados.Count > 0)
        {
            await importRepository.AddImparticionesAsync(creados, cancellationToken);
        }

        return ApplicationResult.Ok(new CsvImportResultDto
        {
            Creados = creados.Count,
            Omitidos = omitidos.Count,
            Errores = errores,
            Detalles = omitidos
        });
    }

    private static IEnumerable<CsvRow> ParseCsv(string text)
    {
        var lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        var first = true;
        var lineNumber = 0;

        foreach (var raw in lines)
        {
            lineNumber++;
            var line = raw.Trim();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            if (first)
            {
                first = false;
                continue;
            }

            yield return new CsvRow(lineNumber, line.Split(',').Select(c => c.Trim().Trim('"')).ToArray());
        }
    }

    private static string ToAsignaturaKey(int cursoId, string nombre)
        => $"{cursoId}:{nombre.Trim().ToLowerInvariant()}";
}
