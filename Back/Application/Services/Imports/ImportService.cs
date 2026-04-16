using Back.Api.Application.Common;
using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Abstractions.Security;
using Back.Api.Application.Dtos;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Back.Api.Application.Services;

public class ImportService(IImportDomainRepository importRepository, IPasswordService passwordService) : IImportService
{
    private static readonly Regex DniRegex = new(@"^\d{8}[TRWAGMYFPDXBNJZSQVHLCKE]$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex TelefonoRegex = new(@"^[6-9]\d{8}$", RegexOptions.Compiled);
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
        var creados = new List<(string Nombre, string Correo, string ContrasenaHash, string Apellidos, string DNI, string Telefono, string Especialidad)>();
        var errores = new List<string>();
        var correos = (await importRepository.GetProfesoresAsync(cancellationToken))
            .Select(p => p.Correo)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var row in ParseCsv(csvText))
        {
            if (row.Columns.Length < 7)
            {
                errores.Add($"Linea {row.LineNumber}: se esperaban las columnas nombre,apellidos,dni,telefono,especialidad,correo,contrasena.");
                continue;
            }

            var nombre = row.Columns[0].Trim();
            var apellidos = row.Columns[1].Trim();
            var dni = row.Columns[2].Trim();
            var telefono = row.Columns[3].Trim();
            var especialidad = row.Columns[4].Trim();
            var correo = row.Columns[5].Trim().ToLowerInvariant();
            var contrasena = row.Columns[6].Trim();

            if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(contrasena))
            {
                errores.Add($"Linea {row.LineNumber}: datos incompletos para '{nombre}'.");
                continue;
            }

            if (!DniRegex.IsMatch(dni))
            {
                errores.Add($"Linea {row.LineNumber}: DNI '{dni}' con formato invalido (debe ser 8 digitos + letra valida).");
                continue;
            }

            if (!TelefonoRegex.IsMatch(telefono))
            {
                errores.Add($"Linea {row.LineNumber}: telefono '{telefono}' con formato invalido (debe ser 9 digitos empezando por 6-9).");
                continue;
            }

            if (!correos.Add(correo))
            {
                errores.Add($"Linea {row.LineNumber}: correo duplicado {correo}.");
                continue;
            }

            creados.Add((nombre, correo, passwordService.Hash(contrasena), apellidos, dni, telefono, especialidad));
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
        var creados = new List<(string Nombre, string Correo, string ContrasenaHash, int CursoId, string Apellidos, string DNI, string Telefono, DateOnly FechaNacimiento)>();
        var errores = new List<string>();
        var cursos = (await importRepository.GetCursosAsync(cancellationToken))
            .ToDictionary(c => c.Nombre, c => c, StringComparer.OrdinalIgnoreCase);
        var correos = (await importRepository.GetEstudiantesAsync(cancellationToken))
            .Select(e => e.Correo)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var row in ParseCsv(csvText))
        {
            if (row.Columns.Length < 8)
            {
                errores.Add($"Linea {row.LineNumber}: se esperaban las columnas nombre,apellidos,dni,telefono,fechaNacimiento,correo,contrasena,cursoNombre.");
                continue;
            }

            var nombre = row.Columns[0].Trim();
            var apellidos = row.Columns[1].Trim();
            var dni = row.Columns[2].Trim();
            var telefono = row.Columns[3].Trim();
            var fechaNacimientoStr = row.Columns[4].Trim();
            var correo = row.Columns[5].Trim().ToLowerInvariant();
            var contrasena = row.Columns[6].Trim();
            var cursoNombre = row.Columns[7].Trim();

            if (!DateOnly.TryParse(fechaNacimientoStr, out var fechaNacimiento))
            {
                errores.Add($"Linea {row.LineNumber}: fecha de nacimiento invalida '{fechaNacimientoStr}'.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(contrasena))
            {
                errores.Add($"Linea {row.LineNumber}: datos incompletos para '{nombre}'.");
                continue;
            }

            if (!DniRegex.IsMatch(dni))
            {
                errores.Add($"Linea {row.LineNumber}: DNI '{dni}' con formato invalido (debe ser 8 digitos + letra valida).");
                continue;
            }

            if (!TelefonoRegex.IsMatch(telefono))
            {
                errores.Add($"Linea {row.LineNumber}: telefono '{telefono}' con formato invalido (debe ser 9 digitos empezando por 6-9).");
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

            creados.Add((nombre, correo, passwordService.Hash(contrasena), curso.Id, apellidos, dni, telefono, fechaNacimiento));
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

    public async Task<ApplicationResult> ImportarTareasAsync(string csvText, CancellationToken cancellationToken = default)
    {
        var errores = new List<string>();
        var omitidos = new List<string>();
        var creados = new List<(string Nombre, int Trimestre, int AsignaturaId, int ProfesorId)>();

        var profesores = (await importRepository.GetProfesoresAsync(cancellationToken))
            .ToDictionary(p => p.Correo, p => p, StringComparer.OrdinalIgnoreCase);
        var cursos = (await importRepository.GetCursosAsync(cancellationToken))
            .ToDictionary(c => c.Nombre, c => c, StringComparer.OrdinalIgnoreCase);
        var asignaturas = await importRepository.GetAsignaturasAsync(cancellationToken);
        var imparticiones = (await importRepository.GetImparticionesAsync(cancellationToken)).ToHashSet();
        var tareasExistentes = (await importRepository.GetTareasAsync(cancellationToken))
            .Select(t => ToTareaKey(t.AsignaturaId, t.Trimestre, t.Nombre))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var row in ParseCsv(csvText))
        {
            if (row.Columns.Length < 5)
            {
                errores.Add($"Linea {row.LineNumber}: se esperaban las columnas profesorCorreo,asignaturaNombre,cursoNombre,trimestre,tareaNombre.");
                continue;
            }

            var profesorCorreo = row.Columns[0].Trim().ToLowerInvariant();
            var asignaturaNombre = row.Columns[1].Trim();
            var cursoNombre = row.Columns[2].Trim();
            var trimestreRaw = row.Columns[3].Trim();
            var tareaNombre = row.Columns[4].Trim();

            if (string.IsNullOrWhiteSpace(profesorCorreo)
                || string.IsNullOrWhiteSpace(asignaturaNombre)
                || string.IsNullOrWhiteSpace(cursoNombre)
                || string.IsNullOrWhiteSpace(trimestreRaw)
                || string.IsNullOrWhiteSpace(tareaNombre))
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

            if (!int.TryParse(trimestreRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var trimestre) || trimestre < 1 || trimestre > 3)
            {
                errores.Add($"Linea {row.LineNumber}: trimestre no valido '{trimestreRaw}'. Debe ser 1, 2 o 3.");
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

            if (!imparticiones.Contains(new ImportImparticionLookup(profesor.Id, asignatura.Id, curso.Id)))
            {
                errores.Add($"Linea {row.LineNumber}: el profesor '{profesorCorreo}' no imparte '{asignaturaNombre}' en '{cursoNombre}'.");
                continue;
            }

            var tareaKey = ToTareaKey(asignatura.Id, trimestre, tareaNombre);
            if (!tareasExistentes.Add(tareaKey))
            {
                omitidos.Add($"{tareaNombre} ({asignaturaNombre} - {cursoNombre} T{trimestre})");
                continue;
            }

            creados.Add((tareaNombre, trimestre, asignatura.Id, profesor.Id));
        }

        if (errores.Count > 0)
        {
            return ApplicationResult.BadRequest(new CsvImportResultDto
            {
                Detail = "La importacion de tareas ha fallado y se ha cancelado.",
                Mensaje = "La importacion de tareas ha fallado y se ha cancelado.",
                Creados = 0,
                Omitidos = omitidos.Count,
                Errores = errores,
                Detalles = omitidos
            });
        }

        if (creados.Count > 0)
        {
            await importRepository.AddTareasAsync(creados, cancellationToken);
        }

        return ApplicationResult.Ok(new CsvImportResultDto
        {
            Creados = creados.Count,
            Omitidos = omitidos.Count,
            Errores = errores,
            Detalles = omitidos
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

    public async Task<ApplicationResult> ImportarNotasAsync(string csvText, CancellationToken cancellationToken = default)
    {
        var errores = new List<string>();
        var detalles = new List<string>();
        var upserts = new List<(int EstudianteId, int TareaId, decimal Valor)>();
        var clavesNotasCsv = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var profesores = (await importRepository.GetProfesoresAsync(cancellationToken))
            .ToDictionary(p => p.Correo, p => p, StringComparer.OrdinalIgnoreCase);
        var estudiantes = (await importRepository.GetEstudiantesAsync(cancellationToken))
            .ToDictionary(e => e.Correo, e => e, StringComparer.OrdinalIgnoreCase);
        var cursos = (await importRepository.GetCursosAsync(cancellationToken))
            .ToDictionary(c => c.Nombre, c => c, StringComparer.OrdinalIgnoreCase);
        var asignaturas = await importRepository.GetAsignaturasAsync(cancellationToken);
        var matriculas = (await importRepository.GetMatriculasAsync(cancellationToken)).ToHashSet();
        var imparticiones = (await importRepository.GetImparticionesAsync(cancellationToken)).ToHashSet();
        var tareasExistentes = (await importRepository.GetTareasAsync(cancellationToken))
            .ToDictionary(t => ToTareaKey(t.AsignaturaId, t.Trimestre, t.Nombre), t => t, StringComparer.OrdinalIgnoreCase);
        var notasExistentes = (await importRepository.GetNotasAsync(cancellationToken)).ToHashSet();

        foreach (var row in ParseCsv(csvText))
        {
            if (row.Columns.Length < 7)
            {
                errores.Add($"Linea {row.LineNumber}: se esperaban las columnas profesorCorreo,estudianteCorreo,asignaturaNombre,cursoNombre,trimestre,tareaNombre,valor.");
                continue;
            }

            var profesorCorreo = row.Columns[0].Trim().ToLowerInvariant();
            var estudianteCorreo = row.Columns[1].Trim().ToLowerInvariant();
            var asignaturaNombre = row.Columns[2].Trim();
            var cursoNombre = row.Columns[3].Trim();
            var trimestreRaw = row.Columns[4].Trim();
            var tareaNombre = row.Columns[5].Trim();
            var valorRaw = row.Columns[6].Trim();

            if (string.IsNullOrWhiteSpace(profesorCorreo)
                || string.IsNullOrWhiteSpace(estudianteCorreo)
                || string.IsNullOrWhiteSpace(asignaturaNombre)
                || string.IsNullOrWhiteSpace(cursoNombre)
                || string.IsNullOrWhiteSpace(trimestreRaw)
                || string.IsNullOrWhiteSpace(tareaNombre)
                || string.IsNullOrWhiteSpace(valorRaw))
            {
                errores.Add($"Linea {row.LineNumber}: todos los campos son obligatorios.");
                continue;
            }

            if (!profesores.TryGetValue(profesorCorreo, out var profesor))
            {
                errores.Add($"Linea {row.LineNumber}: profesor no encontrado '{profesorCorreo}'.");
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

            if (!int.TryParse(trimestreRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var trimestre) || trimestre < 1 || trimestre > 3)
            {
                errores.Add($"Linea {row.LineNumber}: trimestre no valido '{trimestreRaw}'. Debe ser 1, 2 o 3.");
                continue;
            }

            if (!decimal.TryParse(valorRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var valor) || valor < 0 || valor > 10)
            {
                errores.Add($"Linea {row.LineNumber}: valor no valido '{valorRaw}'. Debe estar entre 0 y 10 usando punto decimal si hace falta.");
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
                errores.Add($"Linea {row.LineNumber}: el estudiante '{estudianteCorreo}' no pertenece al curso '{cursoNombre}'.");
                continue;
            }

            if (!matriculas.Contains((estudiante.Id, asignatura.Id)))
            {
                errores.Add($"Linea {row.LineNumber}: el estudiante '{estudianteCorreo}' no esta matriculado en '{asignaturaNombre}' ({cursoNombre}).");
                continue;
            }

            if (!imparticiones.Contains(new ImportImparticionLookup(profesor.Id, asignatura.Id, curso.Id)))
            {
                errores.Add($"Linea {row.LineNumber}: el profesor '{profesorCorreo}' no imparte '{asignaturaNombre}' en '{cursoNombre}'.");
                continue;
            }

            var tareaKey = ToTareaKey(asignatura.Id, trimestre, tareaNombre);
            if (!tareasExistentes.TryGetValue(tareaKey, out var tarea))
            {
                errores.Add($"Linea {row.LineNumber}: la tarea '{tareaNombre}' no existe en '{asignaturaNombre}' ({cursoNombre}) para el trimestre {trimestre}.");
                continue;
            }

            var notaCsvKey = $"{estudiante.Id}:{tareaKey}";
            if (!clavesNotasCsv.Add(notaCsvKey))
            {
                errores.Add($"Linea {row.LineNumber}: nota duplicada para '{estudianteCorreo}' en la tarea '{tareaNombre}'.");
                continue;
            }

            upserts.Add((estudiante.Id, tarea.Id, decimal.Round(valor, 2)));
        }

        if (errores.Count > 0)
        {
            return ApplicationResult.BadRequest(new CsvImportResultDto
            {
                Detail = "La importacion de notas ha fallado y se ha cancelado.",
                Mensaje = "La importacion de notas ha fallado y se ha cancelado.",
                Creados = 0,
                Errores = errores,
                Detalles = detalles
            });
        }

        var nuevas = 0;
        var actualizadas = 0;

        foreach (var nota in upserts)
        {
            var key = (nota.EstudianteId, nota.TareaId);
            if (notasExistentes.Contains(key))
            {
                actualizadas++;
            }
            else
            {
                nuevas++;
                notasExistentes.Add(key);
            }
        }

        if (upserts.Count > 0)
        {
            await importRepository.UpsertNotasAsync(upserts, cancellationToken);
        }

        if (actualizadas > 0)
        {
            detalles.Add($"Notas actualizadas: {actualizadas}.");
        }

        return ApplicationResult.Ok(new CsvImportResultDto
        {
            Creados = nuevas,
            Omitidos = 0,
            Errores = errores,
            Detalles = detalles
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

    private static string ToTareaKey(int asignaturaId, int trimestre, string nombre)
        => $"{asignaturaId}:{trimestre}:{nombre.Trim().ToLowerInvariant()}";
}
