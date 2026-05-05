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
        var created = new List<string>();
        var skipped = new List<string>();
        var errors = new List<string>();
        var existing = (await importRepository.GetCursosAsync(cancellationToken))
            .Select(course => course.Nombre)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var row in ParseCsv(csvText))
        {
            if (row.Columns.Length < 1 || string.IsNullOrWhiteSpace(row.Columns[0]))
            {
                errors.Add($"Linea {row.LineNumber}: el nombre del course es obligatorio.");
            }
            else
            {
                var nombre = row.Columns[0].Trim();
                if (!existing.Add(nombre))
                    skipped.Add(nombre);
                else
                    created.Add(nombre);
            }
        }

        if (created.Count > 0)
        {
            await importRepository.AddCursosAsync(created, cancellationToken);
        }

        return ApplicationResult.Ok(new CsvImportResultDto
        {
            Creados = created.Count,
            Omitidos = skipped.Count,
            Errores = errors,
            Detalles = skipped
        });
    }

    public async Task<ApplicationResult> ImportarAsignaturasAsync(string csvText, CancellationToken cancellationToken = default)
    {
        var created = new List<(string Nombre, int CursoId)>();
        var errors = new List<string>();
        var courses = (await importRepository.GetCursosAsync(cancellationToken))
            .ToDictionary(c => c.Nombre, c => c, StringComparer.OrdinalIgnoreCase);
        var existingKeys = (await importRepository.GetAsignaturasAsync(cancellationToken))
            .Select(subject => ToAsignaturaKey(subject.CursoId, subject.Nombre))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var row in ParseCsv(csvText))
        {
            if (row.Columns.Length < 2)
            {
                errors.Add($"Linea {row.LineNumber}: se esperaban las columnas nombre,courseName.");
            }
            else
            {
                var nombre = row.Columns[0].Trim();
                var courseName = row.Columns[1].Trim();
                if (!courses.TryGetValue(courseName, out var course))
                {
                    errors.Add($"Linea {row.LineNumber}: course no encontrado '{courseName}'.");
                }
                else
                {
                    var subjectKey = ToAsignaturaKey(course.Id, nombre);
                    if (!existingKeys.Add(subjectKey))
                        errors.Add($"Linea {row.LineNumber}: ya existe '{nombre}' en '{courseName}'.");
                    else
                        created.Add((nombre, course.Id));
                }
            }
        }

        if (errors.Count > 0)
        {
            return ApplicationResult.BadRequest(new CsvImportResultDto
            {
                Detail = "La importacion de subjects ha fallado y se ha cancelado.",
                Mensaje = "La importacion de subjects ha fallado y se ha cancelado.",
                Creados = 0,
                Errores = errors
            });
        }

        if (created.Count > 0)
        {
            await importRepository.AddAsignaturasAsync(created, cancellationToken);
        }

        return ApplicationResult.Ok(new CsvImportResultDto
        {
            Creados = created.Count,
            Errores = errors
        });
    }

    public async Task<ApplicationResult> ImportarProfesoresAsync(string csvText, CancellationToken cancellationToken = default)
    {
        var created = new List<(string Nombre, string Correo, string ContrasenaHash, string Apellidos, string DNI, string Telefono, string Especialidad)>();
        var errors = new List<string>();
        var emails = (await importRepository.GetProfesoresAsync(cancellationToken))
            .Select(p => p.Correo)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var row in ParseCsv(csvText))
        {
            if (row.Columns.Length < 7)
            {
                errors.Add($"Linea {row.LineNumber}: se esperaban las columnas nombre,apellidos,dni,telefono,especialidad,email,password.");
            }
            else
            {
                var nombre = row.Columns[0].Trim();
                var apellidos = row.Columns[1].Trim();
                var dni = row.Columns[2].Trim();
                var telefono = row.Columns[3].Trim();
                var especialidad = row.Columns[4].Trim();
                var email = row.Columns[5].Trim().ToLowerInvariant();
                var password = row.Columns[6].Trim();

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                    errors.Add($"Linea {row.LineNumber}: datos incompletos para '{nombre}'.");
                else if (!DniRegex.IsMatch(dni))
                    errors.Add($"Linea {row.LineNumber}: DNI '{dni}' con formato invalido (debe ser 8 digitos + letra valida).");
                else if (!TelefonoRegex.IsMatch(telefono))
                    errors.Add($"Linea {row.LineNumber}: telefono '{telefono}' con formato invalido (debe ser 9 digitos empezando por 6-9).");
                else if (!emails.Add(email))
                    errors.Add($"Linea {row.LineNumber}: email duplicado {email}.");
                else
                    created.Add((nombre, email, passwordService.Hash(password), apellidos, dni, telefono, especialidad));
            }
        }

        if (created.Count > 0)
        {
            await importRepository.AddProfesoresAsync(created, cancellationToken);
        }

        return ApplicationResult.Ok(new CsvImportResultDto
        {
            Creados = created.Count,
            Errores = errors
        });
    }

    public async Task<ApplicationResult> ImportarEstudiantesAsync(string csvText, CancellationToken cancellationToken = default)
    {
        var created = new List<(string Nombre, string Correo, string ContrasenaHash, int CursoId, string Apellidos, string DNI, string Telefono, DateOnly FechaNacimiento)>();
        var errors = new List<string>();
        var courses = (await importRepository.GetCursosAsync(cancellationToken))
            .ToDictionary(c => c.Nombre, c => c, StringComparer.OrdinalIgnoreCase);
        var emails = (await importRepository.GetEstudiantesAsync(cancellationToken))
            .Select(e => e.Correo)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var row in ParseCsv(csvText))
        {
            if (row.Columns.Length < 8)
            {
                errors.Add($"Linea {row.LineNumber}: se esperaban las columnas nombre,apellidos,dni,telefono,birthDate,email,password,courseName.");
            }
            else
            {
                var nombre = row.Columns[0].Trim();
                var apellidos = row.Columns[1].Trim();
                var dni = row.Columns[2].Trim();
                var telefono = row.Columns[3].Trim();
                var birthDateStr = row.Columns[4].Trim();
                var email = row.Columns[5].Trim().ToLowerInvariant();
                var password = row.Columns[6].Trim();
                var courseName = row.Columns[7].Trim();

                if (!DateOnly.TryParse(birthDateStr, out var birthDate))
                    errors.Add($"Linea {row.LineNumber}: fecha de nacimiento invalida '{birthDateStr}'.");
                else if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                    errors.Add($"Linea {row.LineNumber}: datos incompletos para '{nombre}'.");
                else if (!DniRegex.IsMatch(dni))
                    errors.Add($"Linea {row.LineNumber}: DNI '{dni}' con formato invalido (debe ser 8 digitos + letra valida).");
                else if (!TelefonoRegex.IsMatch(telefono))
                    errors.Add($"Linea {row.LineNumber}: telefono '{telefono}' con formato invalido (debe ser 9 digitos empezando por 6-9).");
                else if (!courses.TryGetValue(courseName, out var course))
                    errors.Add($"Linea {row.LineNumber}: course no encontrado '{courseName}'.");
                else if (!emails.Add(email))
                    errors.Add($"Linea {row.LineNumber}: email duplicado {email}.");
                else
                    created.Add((nombre, email, passwordService.Hash(password), course.Id, apellidos, dni, telefono, birthDate));
            }
        }

        if (errors.Count > 0)
        {
            return ApplicationResult.BadRequest(new CsvImportResultDto
            {
                Detail = "La importacion de students ha fallado y se ha cancelado.",
                Mensaje = "La importacion de students ha fallado y se ha cancelado.",
                Creados = 0,
                Errores = errors
            });
        }

        if (created.Count > 0)
        {
            await importRepository.AddEstudiantesAsync(created, cancellationToken);
        }

        return ApplicationResult.Ok(new CsvImportResultDto
        {
            Creados = created.Count,
            Errores = errors
        });
    }

    public async Task<ApplicationResult> ImportarTareasAsync(string csvText, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var skipped = new List<string>();
        var created = new List<(string Nombre, int Trimestre, int AsignaturaId, int ProfesorId)>();

        var teachers = (await importRepository.GetProfesoresAsync(cancellationToken))
            .ToDictionary(p => p.Correo, p => p, StringComparer.OrdinalIgnoreCase);
        var courses = (await importRepository.GetCursosAsync(cancellationToken))
            .ToDictionary(c => c.Nombre, c => c, StringComparer.OrdinalIgnoreCase);
        var subjects = await importRepository.GetAsignaturasAsync(cancellationToken);
        var assignments = (await importRepository.GetImparticionesAsync(cancellationToken)).ToHashSet();
        var existingTasks = (await importRepository.GetTareasAsync(cancellationToken))
            .Select(task => ToTareaKey(task.AsignaturaId, task.Trimestre, task.Nombre))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var row in ParseCsv(csvText))
        {
            if (row.Columns.Length < 5)
            {
                errors.Add($"Linea {row.LineNumber}: se esperaban las columnas teacherEmail,subjectName,courseName,term,tareaNombre.");
            }
            else
            {
                var teacherEmail = row.Columns[0].Trim().ToLowerInvariant();
                var subjectName = row.Columns[1].Trim();
                var courseName = row.Columns[2].Trim();
                var termRaw = row.Columns[3].Trim();
                var tareaNombre = row.Columns[4].Trim();

                if (string.IsNullOrWhiteSpace(teacherEmail)
                    || string.IsNullOrWhiteSpace(subjectName)
                    || string.IsNullOrWhiteSpace(courseName)
                    || string.IsNullOrWhiteSpace(termRaw)
                    || string.IsNullOrWhiteSpace(tareaNombre))
                {
                    errors.Add($"Linea {row.LineNumber}: todos los campos son obligatorios.");
                }
                else if (!teachers.TryGetValue(teacherEmail, out var teacher))
                {
                    errors.Add($"Linea {row.LineNumber}: teacher no encontrado '{teacherEmail}'.");
                }
                else if (!courses.TryGetValue(courseName, out var course))
                {
                    errors.Add($"Linea {row.LineNumber}: course no encontrado '{courseName}'.");
                }
                else if (!int.TryParse(termRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var term) || term < 1 || term > 3)
                {
                    errors.Add($"Linea {row.LineNumber}: term no valido '{termRaw}'. Debe ser 1, 2 o 3.");
                }
                else
                {
                    var subject = subjects.FirstOrDefault(a =>
                        a.CursoId == course.Id &&
                        a.Nombre.Equals(subjectName, StringComparison.OrdinalIgnoreCase));

                    if (subject is null)
                    {
                        errors.Add($"Linea {row.LineNumber}: subject no encontrada '{subjectName}' en '{courseName}'.");
                    }
                    else if (!assignments.Contains(new ImportImparticionLookup(teacher.Id, subject.Id, course.Id)))
                    {
                        errors.Add($"Linea {row.LineNumber}: el teacher '{teacherEmail}' no imparte '{subjectName}' en '{courseName}'.");
                    }
                    else
                    {
                        var taskKey = ToTareaKey(subject.Id, term, tareaNombre);
                        if (!existingTasks.Add(taskKey))
                            skipped.Add($"{tareaNombre} ({subjectName} - {courseName} T{term})");
                        else
                            created.Add((tareaNombre, term, subject.Id, teacher.Id));
                    }
                }
            }
        }

        if (errors.Count > 0)
        {
            return ApplicationResult.BadRequest(new CsvImportResultDto
            {
                Detail = "La importacion de tasks ha fallado y se ha cancelado.",
                Mensaje = "La importacion de tasks ha fallado y se ha cancelado.",
                Creados = 0,
                Omitidos = skipped.Count,
                Errores = errors,
                Detalles = skipped
            });
        }

        if (created.Count > 0)
        {
            await importRepository.AddTareasAsync(created, cancellationToken);
        }

        return ApplicationResult.Ok(new CsvImportResultDto
        {
            Creados = created.Count,
            Omitidos = skipped.Count,
            Errores = errors,
            Detalles = skipped
        });
    }

    public async Task<ApplicationResult> ImportarMatriculasAsync(string csvText, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var skipped = new List<string>();
        var created = new List<(int EstudianteId, int AsignaturaId)>();

        var students = (await importRepository.GetEstudiantesAsync(cancellationToken))
            .ToDictionary(e => e.Correo, e => e, StringComparer.OrdinalIgnoreCase);
        var courses = (await importRepository.GetCursosAsync(cancellationToken))
            .ToDictionary(c => c.Nombre, c => c, StringComparer.OrdinalIgnoreCase);
        var subjects = await importRepository.GetAsignaturasAsync(cancellationToken);
        var enrollments = (await importRepository.GetMatriculasAsync(cancellationToken)).ToHashSet();

        foreach (var row in ParseCsv(csvText))
        {
            if (row.Columns.Length < 3)
            {
                errors.Add($"Linea {row.LineNumber}: se esperaban las columnas studentEmail,subjectName,courseName.");
            }
            else
            {
                var studentEmail = row.Columns[0].Trim().ToLowerInvariant();
                var subjectName = row.Columns[1].Trim();
                var courseName = row.Columns[2].Trim();

                if (string.IsNullOrWhiteSpace(studentEmail) || string.IsNullOrWhiteSpace(subjectName) || string.IsNullOrWhiteSpace(courseName))
                {
                    errors.Add($"Linea {row.LineNumber}: todos los campos son obligatorios.");
                }
                else if (!students.TryGetValue(studentEmail, out var student))
                {
                    errors.Add($"Linea {row.LineNumber}: student no encontrado '{studentEmail}'.");
                }
                else if (!courses.TryGetValue(courseName, out var course))
                {
                    errors.Add($"Linea {row.LineNumber}: course no encontrado '{courseName}'.");
                }
                else
                {
                    var subject = subjects.FirstOrDefault(a =>
                        a.CursoId == course.Id &&
                        a.Nombre.Equals(subjectName, StringComparison.OrdinalIgnoreCase));

                    if (subject is null)
                    {
                        errors.Add($"Linea {row.LineNumber}: subject no encontrada '{subjectName}' en '{courseName}'.");
                    }
                    else if (student.CursoId != course.Id)
                    {
                        errors.Add($"Linea {row.LineNumber}: el student '{studentEmail}' solo puede matricularse en subjects de su course.");
                    }
                    else
                    {
                        var enrollment = (student.Id, subject.Id);
                        if (!enrollments.Add(enrollment))
                            skipped.Add($"{studentEmail} -> {subjectName} ({courseName})");
                        else
                            created.Add(enrollment);
                    }
                }
            }
        }

        if (errors.Count > 0)
        {
            return ApplicationResult.BadRequest(new CsvImportResultDto
            {
                Detail = "La importacion de enrollments ha fallado y se ha cancelado.",
                Mensaje = "La importacion de enrollments ha fallado y se ha cancelado.",
                Creados = 0,
                Omitidos = skipped.Count,
                Errores = errors,
                Detalles = skipped
            });
        }

        if (created.Count > 0)
        {
            await importRepository.AddMatriculasAsync(created, cancellationToken);
        }

        return ApplicationResult.Ok(new CsvImportResultDto
        {
            Creados = created.Count,
            Omitidos = skipped.Count,
            Errores = errors,
            Detalles = skipped
        });
    }

    public async Task<ApplicationResult> ImportarImparticionesAsync(string csvText, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var skipped = new List<string>();
        var created = new List<(int ProfesorId, int AsignaturaId, int CursoId)>();

        var teachers = (await importRepository.GetProfesoresAsync(cancellationToken))
            .ToDictionary(p => p.Correo, p => p, StringComparer.OrdinalIgnoreCase);
        var courses = (await importRepository.GetCursosAsync(cancellationToken))
            .ToDictionary(c => c.Nombre, c => c, StringComparer.OrdinalIgnoreCase);
        var subjects = await importRepository.GetAsignaturasAsync(cancellationToken);
        var assignments = await importRepository.GetImparticionesAsync(cancellationToken);
        var subjectTeacherMap = assignments
            .GroupBy(assignment => assignment.AsignaturaId)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.First().ProfesorId);
        var combinations = assignments
            .Select(assignment => (assignment.ProfesorId, assignment.AsignaturaId, assignment.CursoId))
            .ToHashSet();

        foreach (var row in ParseCsv(csvText))
        {
            if (row.Columns.Length < 3)
            {
                errors.Add($"Linea {row.LineNumber}: se esperaban las columnas teacherEmail,subjectName,courseName.");
            }
            else
            {
                var teacherEmail = row.Columns[0].Trim().ToLowerInvariant();
                var subjectName = row.Columns[1].Trim();
                var courseName = row.Columns[2].Trim();

                if (string.IsNullOrWhiteSpace(teacherEmail) || string.IsNullOrWhiteSpace(subjectName) || string.IsNullOrWhiteSpace(courseName))
                {
                    errors.Add($"Linea {row.LineNumber}: todos los campos son obligatorios.");
                }
                else if (!teachers.TryGetValue(teacherEmail, out var teacher))
                {
                    errors.Add($"Linea {row.LineNumber}: teacher no encontrado '{teacherEmail}'.");
                }
                else if (!courses.TryGetValue(courseName, out var course))
                {
                    errors.Add($"Linea {row.LineNumber}: course no encontrado '{courseName}'.");
                }
                else
                {
                    var subject = subjects.FirstOrDefault(a =>
                        a.CursoId == course.Id &&
                        a.Nombre.Equals(subjectName, StringComparison.OrdinalIgnoreCase));

                    if (subject is null)
                    {
                        errors.Add($"Linea {row.LineNumber}: subject no encontrada '{subjectName}' en '{courseName}'.");
                    }
                    else if (subjectTeacherMap.TryGetValue(subject.Id, out var currentTeacher) && currentTeacher != teacher.Id)
                    {
                        errors.Add($"Linea {row.LineNumber}: la subject '{subjectName}' en '{courseName}' ya tiene un teacher asignado.");
                    }
                    else
                    {
                        var assignment = (teacher.Id, subject.Id, course.Id);
                        if (!combinations.Add(assignment))
                        {
                            skipped.Add($"{teacherEmail} -> {subjectName} ({courseName})");
                        }
                        else
                        {
                            subjectTeacherMap[subject.Id] = teacher.Id;
                            created.Add(assignment);
                        }
                    }
                }
            }
        }

        if (errors.Count > 0)
        {
            return ApplicationResult.BadRequest(new CsvImportResultDto
            {
                Detail = "La importacion de assignments ha fallado y se ha cancelado.",
                Mensaje = "La importacion de assignments ha fallado y se ha cancelado.",
                Creados = 0,
                Omitidos = skipped.Count,
                Errores = errors,
                Detalles = skipped
            });
        }

        if (created.Count > 0)
        {
            await importRepository.AddImparticionesAsync(created, cancellationToken);
        }

        return ApplicationResult.Ok(new CsvImportResultDto
        {
            Creados = created.Count,
            Omitidos = skipped.Count,
            Errores = errors,
            Detalles = skipped
        });
    }

    public async Task<ApplicationResult> ImportarNotasAsync(string csvText, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var details = new List<string>();
        var upserts = new List<(int EstudianteId, int TareaId, decimal Valor)>();
        var csvGradeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var teachers = (await importRepository.GetProfesoresAsync(cancellationToken))
            .ToDictionary(p => p.Correo, p => p, StringComparer.OrdinalIgnoreCase);
        var students = (await importRepository.GetEstudiantesAsync(cancellationToken))
            .ToDictionary(e => e.Correo, e => e, StringComparer.OrdinalIgnoreCase);
        var courses = (await importRepository.GetCursosAsync(cancellationToken))
            .ToDictionary(c => c.Nombre, c => c, StringComparer.OrdinalIgnoreCase);
        var subjects = await importRepository.GetAsignaturasAsync(cancellationToken);
        var enrollments = (await importRepository.GetMatriculasAsync(cancellationToken)).ToHashSet();
        var assignments = (await importRepository.GetImparticionesAsync(cancellationToken)).ToHashSet();
        var existingTasks = (await importRepository.GetTareasAsync(cancellationToken))
            .ToDictionary(task => ToTareaKey(task.AsignaturaId, task.Trimestre, task.Nombre), task => task, StringComparer.OrdinalIgnoreCase);
        var existingGrades = (await importRepository.GetNotasAsync(cancellationToken)).ToHashSet();

        foreach (var row in ParseCsv(csvText))
        {
            if (row.Columns.Length < 7)
            {
                errors.Add($"Linea {row.LineNumber}: se esperaban las columnas teacherEmail,studentEmail,subjectName,courseName,term,tareaNombre,grade.");
            }
            else
            {
                var teacherEmail = row.Columns[0].Trim().ToLowerInvariant();
                var studentEmail = row.Columns[1].Trim().ToLowerInvariant();
                var subjectName = row.Columns[2].Trim();
                var courseName = row.Columns[3].Trim();
                var termRaw = row.Columns[4].Trim();
                var tareaNombre = row.Columns[5].Trim();
                var gradeRaw = row.Columns[6].Trim();

                if (string.IsNullOrWhiteSpace(teacherEmail)
                    || string.IsNullOrWhiteSpace(studentEmail)
                    || string.IsNullOrWhiteSpace(subjectName)
                    || string.IsNullOrWhiteSpace(courseName)
                    || string.IsNullOrWhiteSpace(termRaw)
                    || string.IsNullOrWhiteSpace(tareaNombre)
                    || string.IsNullOrWhiteSpace(gradeRaw))
                {
                    errors.Add($"Linea {row.LineNumber}: todos los campos son obligatorios.");
                }
                else if (!teachers.TryGetValue(teacherEmail, out var teacher))
                {
                    errors.Add($"Linea {row.LineNumber}: teacher no encontrado '{teacherEmail}'.");
                }
                else if (!students.TryGetValue(studentEmail, out var student))
                {
                    errors.Add($"Linea {row.LineNumber}: student no encontrado '{studentEmail}'.");
                }
                else if (!courses.TryGetValue(courseName, out var course))
                {
                    errors.Add($"Linea {row.LineNumber}: course no encontrado '{courseName}'.");
                }
                else if (!int.TryParse(termRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var term) || term < 1 || term > 3)
                {
                    errors.Add($"Linea {row.LineNumber}: term no valido '{termRaw}'. Debe ser 1, 2 o 3.");
                }
                else if (!decimal.TryParse(gradeRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var grade) || grade < 0 || grade > 10)
                {
                    errors.Add($"Linea {row.LineNumber}: grade no valido '{gradeRaw}'. Debe estar entre 0 y 10 usando punto decimal si hace falta.");
                }
                else
                {
                    var subject = subjects.FirstOrDefault(a =>
                        a.CursoId == course.Id &&
                        a.Nombre.Equals(subjectName, StringComparison.OrdinalIgnoreCase));

                    if (subject is null)
                    {
                        errors.Add($"Linea {row.LineNumber}: subject no encontrada '{subjectName}' en '{courseName}'.");
                    }
                    else if (student.CursoId != course.Id)
                    {
                        errors.Add($"Linea {row.LineNumber}: el student '{studentEmail}' no pertenece al course '{courseName}'.");
                    }
                    else if (!enrollments.Contains((student.Id, subject.Id)))
                    {
                        errors.Add($"Linea {row.LineNumber}: el student '{studentEmail}' no esta matriculado en '{subjectName}' ({courseName}).");
                    }
                    else if (!assignments.Contains(new ImportImparticionLookup(teacher.Id, subject.Id, course.Id)))
                    {
                        errors.Add($"Linea {row.LineNumber}: el teacher '{teacherEmail}' no imparte '{subjectName}' en '{courseName}'.");
                    }
                    else
                    {
                        var taskKey = ToTareaKey(subject.Id, term, tareaNombre);
                        if (!existingTasks.TryGetValue(taskKey, out var task))
                        {
                            errors.Add($"Linea {row.LineNumber}: la task '{tareaNombre}' no existe en '{subjectName}' ({courseName}) para el term {term}.");
                        }
                        else if (!csvGradeKeys.Add($"{student.Id}:{taskKey}"))
                        {
                            errors.Add($"Linea {row.LineNumber}: grade duplicada para '{studentEmail}' en la task '{tareaNombre}'.");
                        }
                        else
                        {
                            upserts.Add((student.Id, task.Id, decimal.Round(grade, 2)));
                        }
                    }
                }
            }
        }

        if (errors.Count > 0)
        {
            return ApplicationResult.BadRequest(new CsvImportResultDto
            {
                Detail = "La importacion de notas ha fallado y se ha cancelado.",
                Mensaje = "La importacion de notas ha fallado y se ha cancelado.",
                Creados = 0,
                Errores = errors,
                Detalles = details
            });
        }

        var newCount = 0;
        var updatedCount = 0;

        foreach (var grade in upserts)
        {
            var key = (grade.EstudianteId, grade.TareaId);
            if (existingGrades.Contains(key))
            {
                updatedCount++;
            }
            else
            {
                newCount++;
                existingGrades.Add(key);
            }
        }

        if (upserts.Count > 0)
        {
            await importRepository.UpsertNotasAsync(upserts, cancellationToken);
        }

        if (updatedCount > 0)
        {
            details.Add($"Notas updatedCount: {updatedCount}.");
        }

        return ApplicationResult.Ok(new CsvImportResultDto
        {
            Creados = newCount,
            Omitidos = 0,
            Errores = errors,
            Detalles = details
        });
    }

    private static IEnumerable<CsvRow> ParseCsv(string text)
    {
        var lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        var isFirstDataRow = true;
        var lineNumber = 0;

        foreach (var raw in lines)
        {
            lineNumber++;
            var line = raw.Trim();
            if (!string.IsNullOrEmpty(line))
            {
                if (isFirstDataRow)
                {
                    isFirstDataRow = false;
                }
                else
                {
                    yield return new CsvRow(lineNumber, line.Split(',').Select(col => col.Trim().Trim('"')).ToArray());
                }
            }
        }
    }

    private static string ToAsignaturaKey(int cursoId, string nombre)
        => $"{cursoId}:{nombre.Trim().ToLowerInvariant()}";

    private static string ToTareaKey(int asignaturaId, int term, string nombre)
        => $"{asignaturaId}:{term}:{nombre.Trim().ToLowerInvariant()}";
}
