using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Abstractions.Security;
using Back.Api.Application.Configuration;
using Back.Api.Persistence.Context;
using Back.Api.Application.Dtos;
using Back.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Back.Api.Persistence.Repositories;

public class ProfesoresDomainRepository(AppDbContext context, ICurrentSchoolContext currentSchoolContext) : IProfesoresDomainRepository
{
    private sealed record ProfesorAlumnoStatsRow(int EstudianteId, string Alumno, int AsignaturaId);
    private sealed record ProfesorTareaStatsRow(int TareaId, string Nombre, int Trimestre, int AsignaturaId, int ProfesorId);

    #region Checks

    public Task<bool> ProfesorExisteAsync(int profesorId, CancellationToken cancellationToken = default)
        => context.Profesores.AnyAsync(p => p.Id == profesorId, cancellationToken);

    public Task<bool> ProfesorImparteAsignaturaAsync(int profesorId, int subjectId, CancellationToken cancellationToken = default)
        => context.ProfesorAsignaturaCursos
            .AnyAsync(pac => pac.ProfesorId == profesorId && pac.AsignaturaId == subjectId, cancellationToken);

    public Task<bool> ProfesorImparteTareaAsync(int profesorId, int tareaId, CancellationToken cancellationToken = default)
        => context.Tareas.AnyAsync(t => t.Id == tareaId && t.ProfesorId == profesorId, cancellationToken);

    public Task<bool> CorreoDuplicadoAsync(string correo, CancellationToken cancellationToken = default)
        => context.Cuentas.AnyAsync(c => c.Correo == correo && c.ColegioId == currentSchoolContext.SchoolId, cancellationToken);

    public Task<bool> CorreoDuplicadoExceptAsync(string correo, int exceptProfesorId, CancellationToken cancellationToken = default)
        => context.Cuentas.AnyAsync(c => c.Correo == correo && c.ColegioId == currentSchoolContext.SchoolId && (c.Profesor == null || c.Profesor.Id != exceptProfesorId), cancellationToken);

    public Task<bool> CursoExisteAsync(int cursoId, CancellationToken cancellationToken = default)
        => context.Cursos.AnyAsync(c => c.Id == cursoId, cancellationToken);

    public Task<bool> AsignaturaYaTieneOtroProfesorAsync(int subjectId, int profesorId, CancellationToken cancellationToken = default)
        => context.ProfesorAsignaturaCursos
            .AnyAsync(assignment => assignment.AsignaturaId == subjectId && assignment.ProfesorId != profesorId, cancellationToken);

    public Task<bool> ImparticionExisteAsync(int profesorId, int subjectId, int cursoId, CancellationToken cancellationToken = default)
        => context.ProfesorAsignaturaCursos
            .AnyAsync(assignment => assignment.ProfesorId == profesorId && assignment.AsignaturaId == subjectId && assignment.CursoId == cursoId, cancellationToken);

    public Task<bool> EstudianteMatriculadoAsync(int estudianteId, int subjectId, CancellationToken cancellationToken = default)
        => context.EstudianteAsignaturas
            .AnyAsync(matricula => matricula.EstudianteId == estudianteId && matricula.AsignaturaId == subjectId, cancellationToken);

    public Task<bool> ProfesorImparteAlCursoAsync(int profesorId, int subjectId, int cursoId, CancellationToken cancellationToken = default)
        => context.ProfesorAsignaturaCursos
            .AnyAsync(assignment => assignment.ProfesorId == profesorId && assignment.AsignaturaId == subjectId && assignment.CursoId == cursoId, cancellationToken);

    public Task<bool> TareaDuplicadaAsync(int subjectId, int trimestre, string nombre, CancellationToken cancellationToken = default)
        => context.Tareas
            .AnyAsync(t => t.AsignaturaId == subjectId && t.Trimestre == trimestre && t.Nombre == nombre, cancellationToken);

    #endregion

    #region Simple lookups

    public async Task<AsignaturaInfoDto?> GetAsignaturaInfoAsync(int subjectId, CancellationToken cancellationToken = default)
        => await context.Asignaturas
            .AsNoTracking()
            .Where(a => a.Id == subjectId)
            .Select(a => new AsignaturaInfoDto { Id = a.Id, Nombre = a.Nombre, CursoId = a.CursoId, Curso = a.Curso!.Nombre })
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<TareaResumenDto?> GetTareaResumenAsync(int tareaId, CancellationToken cancellationToken = default)
        => await context.Tareas
            .AsNoTracking()
            .Where(t => t.Id == tareaId)
            .Select(t => new TareaResumenDto { TareaId = t.Id, Nombre = t.Nombre, Trimestre = t.Trimestre })
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<(int Id, int CursoId)?> GetAsignaturaBasicaAsync(int subjectId, CancellationToken cancellationToken = default)
    {
        var subject = await context.Asignaturas
            .AsNoTracking()
            .Where(a => a.Id == subjectId)
            .Select(a => new { a.Id, a.CursoId })
            .FirstOrDefaultAsync(cancellationToken);

        return subject is null ? null : (subject.Id, subject.CursoId);
    }

    public async Task<(int Id, int AsignaturaId, int ProfesorId)?> GetTareaInfoAsync(int tareaId, CancellationToken cancellationToken = default)
    {
        var task = await context.Tareas
            .AsNoTracking()
            .Where(t => t.Id == tareaId)
            .Select(t => new { t.Id, t.AsignaturaId, t.ProfesorId })
            .FirstOrDefaultAsync(cancellationToken);

        return task is null ? null : (task.Id, task.AsignaturaId, task.ProfesorId);
    }

    public async Task<int?> GetEstudianteCursoAsync(int estudianteId, CancellationToken cancellationToken = default)
    {
        var cursoId = await context.Estudiantes
            .AsNoTracking()
            .Where(e => e.Id == estudianteId)
            .Select(e => (int?)e.CursoId)
            .FirstOrDefaultAsync(cancellationToken);

        return cursoId;
    }

    #endregion

    #region Queries

    public async Task<IEnumerable<ProfesorLookupDto>> GetSimpleProfesoresAsync(CancellationToken cancellationToken = default)
        => await context.Profesores
            .AsNoTracking()
            .OrderBy(p => p.Nombre)
            .Select(p => new ProfesorLookupDto
            {
                Id = p.Id,
                Nombre = p.Nombre
            })
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<ProfesorListItemDto>> GetAllProfesoresAsync(CancellationToken cancellationToken = default)
        => await context.Profesores
            .AsNoTracking()
            .Select(teacher => new ProfesorListItemDto
            {
                Id = teacher.Id,
                Nombre = teacher.Nombre,
                Apellidos = teacher.Apellidos,
                DNI = teacher.DNI,
                Telefono = teacher.Telefono,
                Especialidad = teacher.Especialidad,
                Correo = teacher.Cuenta!.Correo,
                Imparticiones = teacher.ProfesorAsignaturaCursos.Select(assignment => new ProfesorImparticionDto
                {
                    AsignaturaId = assignment.AsignaturaId,
                    Asignatura = assignment.Asignatura!.Nombre,
                    CursoId = assignment.CursoId,
                    Curso = assignment.Curso!.Nombre
                }).ToList()
            })
            .ToListAsync(cancellationToken);

    public async Task<ProfesorDetalleDto?> GetDetalleAsync(int profesorId, CancellationToken cancellationToken = default)
    {
        var teacher = await context.Profesores
            .AsNoTracking()
            .Where(p => p.Id == profesorId)
            .Select(p => new { p.Id, p.Nombre, p.Apellidos, p.DNI, p.Telefono, p.Especialidad, Correo = p.Cuenta!.Correo })
            .FirstOrDefaultAsync(cancellationToken);

        if (teacher is null) return null;

        var cursos = await context.ProfesorAsignaturaCursos
            .AsNoTracking()
            .Where(i => i.ProfesorId == profesorId)
            .OrderBy(i => i.Curso!.Nombre).ThenBy(i => i.Asignatura!.Nombre)
            .Select(i => new { CursoId = i.CursoId, Curso = i.Curso!.Nombre, AsignaturaId = i.AsignaturaId, Asignatura = i.Asignatura!.Nombre })
            .ToListAsync(cancellationToken);

        return new ProfesorDetalleDto
        {
            Id = teacher.Id,
            Nombre = teacher.Nombre,
            Apellidos = teacher.Apellidos,
            DNI = teacher.DNI,
            Telefono = teacher.Telefono,
            Especialidad = teacher.Especialidad,
            Correo = teacher.Correo,
            Cursos = cursos
                .GroupBy(courseSubject => new { courseSubject.CursoId, courseSubject.Curso })
                .Select(courseGroup => new ProfesorCursoPanelDto
                {
                    CursoId = courseGroup.Key.CursoId,
                    Curso = courseGroup.Key.Curso,
                    Asignaturas = courseGroup.Select(subject => new ProfesorCursoAsignaturaDto { AsignaturaId = subject.AsignaturaId, Nombre = subject.Asignatura }).ToList()
                })
                .OrderBy(course => course.Curso)
                .ToList()
        };
    }

    public async Task<ProfesorPanelDto?> GetPanelAsync(int profesorId, CancellationToken cancellationToken = default)
    {
        var teacher = await context.Profesores
            .AsNoTracking()
            .Where(p => p.Id == profesorId)
            .Select(p => new { p.Id, p.Nombre })
            .FirstOrDefaultAsync(cancellationToken);

        if (teacher is null) return null;

        var cursos = await context.ProfesorAsignaturaCursos
            .AsNoTracking()
            .Where(i => i.ProfesorId == profesorId)
            .OrderBy(i => i.Curso!.Nombre).ThenBy(i => i.Asignatura!.Nombre)
            .Select(i => new { CursoId = i.CursoId, Curso = i.Curso!.Nombre, AsignaturaId = i.AsignaturaId, Asignatura = i.Asignatura!.Nombre })
            .ToListAsync(cancellationToken);

        return new ProfesorPanelDto
        {
            Id = teacher.Id,
            Nombre = teacher.Nombre,
            Cursos = cursos
                .GroupBy(courseSubject => new { courseSubject.CursoId, courseSubject.Curso })
                .Select(courseGroup => new ProfesorCursoPanelDto
                {
                    CursoId = courseGroup.Key.CursoId,
                    Curso = courseGroup.Key.Curso,
                    Asignaturas = courseGroup.Select(subject => new ProfesorCursoAsignaturaDto { AsignaturaId = subject.AsignaturaId, Nombre = subject.Asignatura }).ToList()
                })
                .OrderBy(course => course.Curso)
                .ToList()
        };
    }

    public async Task<AsignaturaAlumnosResponseDto?> GetAlumnosCompletoAsync(int subjectId, CancellationToken cancellationToken = default)
    {
        var subjectInfo = await GetAsignaturaInfoAsync(subjectId, cancellationToken);
        if (subjectInfo is null) return null;

        var tasks = await GetTareasDeAsignaturaAsync(subjectId, cancellationToken);
        var taskIds = tasks.Select(t => t.TareaId).ToList();

        var studentsRaw = await context.EstudianteAsignaturas
            .AsNoTracking()
            .Where(ea => ea.AsignaturaId == subjectId)
            .OrderBy(ea => ea.Estudiante!.Nombre)
            .Select(ea => new { ea.EstudianteId, Alumno = ea.Estudiante!.Nombre })
            .ToListAsync(cancellationToken);

        var studentIds = studentsRaw.Select(studentRecord => studentRecord.EstudianteId).ToList();
        var allGrades = await context.Notas
            .AsNoTracking()
            .Where(grade => studentIds.Contains(grade.EstudianteId) && taskIds.Contains(grade.TareaId))
            .ToListAsync(cancellationToken);

        var students = studentsRaw.Select(studentRecord =>
        {
            var studentGrades = allGrades.Where(grade => grade.EstudianteId == studentRecord.EstudianteId).ToList();
            var gradesList = tasks.Select(task => new AsignaturaNotaAlumnoDto
            {
                TareaId = task.TareaId,
                Valor = studentGrades.FirstOrDefault(grade => grade.TareaId == task.TareaId)?.Valor
            }).ToList();

            decimal? Media(int trim)
            {
                var values = tasks.Where(task => task.Trimestre == trim)
                    .Select(task => gradesList.First(grade => grade.TareaId == task.TareaId).Valor)
                    .Where(value => value.HasValue).Select(value => value!.Value).ToList();
                return values.Count > 0 ? values.Average() : null;
            }

            var t1 = Media(1); var t2 = Media(2); var t3 = Media(3);
            decimal? finalGrade = (t1.HasValue && t2.HasValue && t3.HasValue)
                ? Math.Round((t1.Value + t2.Value + t3.Value) / 3, 2) : null;

            return new AsignaturaAlumnoDto
            {
                EstudianteId = studentRecord.EstudianteId,
                Alumno = studentRecord.Alumno,
                Notas = gradesList,
                Medias = new MediasTrimestralesDto
                {
                    T1 = t1.HasValue ? Math.Round(t1.Value, 2) : null,
                    T2 = t2.HasValue ? Math.Round(t2.Value, 2) : null,
                    T3 = t3.HasValue ? Math.Round(t3.Value, 2) : null
                },
                NotaFinal = finalGrade
            };
        }).ToList();

        return new AsignaturaAlumnosResponseDto { Asignatura = subjectInfo, Tareas = tasks, Alumnos = students };
    }

    public async Task<List<TareaResumenDto>> GetTareasDeAsignaturaAsync(int subjectId, CancellationToken cancellationToken = default)
        => await context.Tareas
            .AsNoTracking()
            .Where(t => t.AsignaturaId == subjectId)
            .OrderBy(t => t.Trimestre).ThenBy(t => t.Nombre)
            .Select(t => new TareaResumenDto { TareaId = t.Id, Nombre = t.Nombre, Trimestre = t.Trimestre })
            .ToListAsync(cancellationToken);

    public async Task<List<ProfesorAlumnoResumenRow>> GetAlumnosResumenAsync(int subjectId, CancellationToken cancellationToken = default)
        => await context.EstudianteAsignaturas
            .AsNoTracking()
            .Where(ea => ea.AsignaturaId == subjectId)
            .OrderBy(ea => ea.Estudiante!.Nombre)
            .Select(ea => new ProfesorAlumnoResumenRow(ea.EstudianteId, ea.Estudiante!.Nombre))
            .ToListAsync(cancellationToken);

    public async Task<AsignaturaAlumnosResumenResponseDto?> GetAlumnosResumenResponseAsync(int subjectId, CancellationToken cancellationToken = default)
    {
        var subjectInfo = await GetAsignaturaInfoAsync(subjectId, cancellationToken);
        if (subjectInfo is null) return null;

        var tasks = await GetTareasDeAsignaturaAsync(subjectId, cancellationToken);
        var students = await GetAlumnosResumenAsync(subjectId, cancellationToken);
        var studentIds = students.Select(a => a.EstudianteId).ToList();
        var taskIds = tasks.Select(t => t.TareaId).ToList();

        var gradeMap = await context.Notas
            .AsNoTracking()
            .Where(n => studentIds.Contains(n.EstudianteId) && taskIds.Contains(n.TareaId))
            .ToDictionaryAsync(n => (n.EstudianteId, n.TareaId), n => (decimal?)n.Valor, cancellationToken);

        return new AsignaturaAlumnosResumenResponseDto
        {
            Asignatura = subjectInfo,
            Tareas = tasks,
            Alumnos = students
                .Select(student => BuildAlumnoResumen(student.EstudianteId, student.Alumno, tasks, gradeMap))
                .ToList()
        };
    }

    public async Task<List<ProfesorTareaCalificacionRow>> GetCalificacionesTareaAsync(int subjectId, int tareaId, CancellationToken cancellationToken = default)
    {
        var students = await GetAlumnosResumenAsync(subjectId, cancellationToken);
        var grades = await context.Notas
            .AsNoTracking()
            .Where(n => n.TareaId == tareaId)
            .ToDictionaryAsync(n => n.EstudianteId, n => (decimal?)n.Valor, cancellationToken);
        return students.Select(alumnoResumen => new ProfesorTareaCalificacionRow(
            alumnoResumen.EstudianteId, alumnoResumen.Alumno,
            grades.TryGetValue(alumnoResumen.EstudianteId, out var notaValor) ? notaValor : null
        )).ToList();
    }

    public async Task<AsignaturaCalificacionesTareaResponseDto?> GetCalificacionesTareaResponseAsync(int subjectId, int tareaId, CancellationToken cancellationToken = default)
    {
        var task = await GetTareaResumenAsync(tareaId, cancellationToken);
        if (task is null)
            return null;

        var calificaciones = await GetCalificacionesTareaAsync(subjectId, tareaId, cancellationToken);
        return new AsignaturaCalificacionesTareaResponseDto
        {
            TareaId = task.TareaId,
            Tarea = task.Nombre,
            Trimestre = task.Trimestre,
            Calificaciones = calificaciones.Select(c => new AsignaturaCalificacionTareaDto
            {
                EstudianteId = c.EstudianteId,
                Alumno = c.Alumno,
                Valor = c.Valor
            }).ToList()
        };
    }

    public async Task<ProfesorAlumnoDetalleDto?> GetAlumnoDetalleAsync(int subjectId, int estudianteId, CancellationToken cancellationToken = default)
    {
        var student = await context.Estudiantes
            .AsNoTracking()
            .Where(e => e.Id == estudianteId)
            .Select(e => new { e.Id, e.Nombre })
            .FirstOrDefaultAsync(cancellationToken);

        if (student is null) return null;

        var isMatriculado = await context.EstudianteAsignaturas
            .AnyAsync(ea => ea.EstudianteId == estudianteId && ea.AsignaturaId == subjectId, cancellationToken);
        if (!isMatriculado) return null;

        var tasks = await GetTareasDeAsignaturaAsync(subjectId, cancellationToken);
        var taskIds = tasks.Select(t => t.TareaId).ToList();
        var grades = await context.Notas
            .AsNoTracking()
            .Where(n => n.EstudianteId == estudianteId && taskIds.Contains(n.TareaId))
            .ToListAsync(cancellationToken);

        var gradesList = tasks.Select(task => new AsignaturaNotaAlumnoDto
        {
            TareaId = task.TareaId,
            Valor = grades.FirstOrDefault(grade => grade.TareaId == task.TareaId)?.Valor
        }).ToList();

        decimal? Media(int trim)
        {
            var values = tasks.Where(task => task.Trimestre == trim)
                .Select(task => gradesList.First(grade => grade.TareaId == task.TareaId).Valor)
                .Where(value => value.HasValue).Select(value => value!.Value).ToList();
            return values.Count > 0 ? Math.Round(values.Average(), 2) : null;
        }

        var t1 = Media(1); var t2 = Media(2); var t3 = Media(3);
        return new ProfesorAlumnoDetalleDto
        {
            EstudianteId = student.Id,
            Alumno = student.Nombre,
            Notas = gradesList,
            Medias = new MediasTrimestralesDto { T1 = t1, T2 = t2, T3 = t3 },
            NotaFinal = (t1.HasValue && t2.HasValue && t3.HasValue)
                ? Math.Round((t1.Value + t2.Value + t3.Value) / 3, 2) : null
        };
    }

    public async Task<IEnumerable<TareaResumenDto>> GetTareasDeProfesorEnAsignaturaAsync(int profesorId, int subjectId, CancellationToken cancellationToken = default)
        => await context.Tareas
            .AsNoTracking()
            .Where(t => t.AsignaturaId == subjectId && t.ProfesorId == profesorId)
            .OrderBy(t => t.Trimestre).ThenBy(t => t.Nombre)
            .Select(t => new TareaResumenDto { TareaId = t.Id, Nombre = t.Nombre, Trimestre = t.Trimestre })
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<TareaConNotasDto>> GetTareasConNotasAsync(int subjectId, CancellationToken cancellationToken = default)
    {
        var subjectName = await context.Asignaturas
            .AsNoTracking()
            .Where(a => a.Id == subjectId)
            .Select(a => a.Nombre)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var tasks = await context.Tareas
            .AsNoTracking()
            .Where(t => t.AsignaturaId == subjectId)
            .OrderBy(t => t.Trimestre).ThenBy(t => t.Nombre)
            .ToListAsync(cancellationToken);

        var taskIds = tasks.Select(t => t.Id).ToList();
        var grades = await context.Notas
            .AsNoTracking()
            .Where(n => taskIds.Contains(n.TareaId))
            .Select(grade => new { grade.TareaId, grade.EstudianteId, Alumno = grade.Estudiante!.Nombre, grade.Valor })
            .ToListAsync(cancellationToken);

        return tasks.Select(task => new TareaConNotasDto
        {
            TareaId = task.Id,
            Nombre = task.Nombre,
            Trimestre = task.Trimestre,
            AsignaturaId = task.AsignaturaId,
            Asignatura = subjectName,
            Notas = grades
                .Where(grade => grade.TareaId == task.Id)
                .OrderBy(grade => grade.Alumno)
                .Select(grade => new TareaNotaAlumnoDto { EstudianteId = grade.EstudianteId, Alumno = grade.Alumno, Valor = grade.Valor })
                .ToList()
        });
    }

    public async Task<ProfesorStatsDto?> GetStatsAsync(int profesorId, CancellationToken cancellationToken = default)
    {
        var teacher = await context.Profesores
            .AsNoTracking()
            .Where(p => p.Id == profesorId)
            .Select(p => new { p.Id, p.Nombre })
            .FirstOrDefaultAsync(cancellationToken);

        if (teacher is null)
            return null;

        var assignments = await context.ProfesorAsignaturaCursos
            .AsNoTracking()
            .Where(assignment => assignment.ProfesorId == profesorId)
            .OrderBy(assignment => assignment.Curso!.Nombre)
            .ThenBy(assignment => assignment.Asignatura!.Nombre)
            .Select(assignment => new { assignment.AsignaturaId, Asignatura = assignment.Asignatura!.Nombre, Curso = assignment.Curso!.Nombre })
            .ToListAsync(cancellationToken);

        var subjectIds = assignments.Select(assignment => assignment.AsignaturaId).Distinct().ToList();
        if (subjectIds.Count == 0)
        {
            return new ProfesorStatsDto
            {
                ProfesorId = teacher.Id,
                Nombre = teacher.Nombre,
                MediaGlobal = null,
                Asignaturas = []
            };
        }

        var students = await context.EstudianteAsignaturas
            .AsNoTracking()
            .Where(ea => subjectIds.Contains(ea.AsignaturaId))
            .OrderBy(ea => ea.Estudiante!.Nombre)
            .Select(ea => new ProfesorAlumnoStatsRow(ea.EstudianteId, ea.Estudiante!.Nombre, ea.AsignaturaId))
            .ToListAsync(cancellationToken);

        var tasks = await context.Tareas
            .AsNoTracking()
            .Where(t => subjectIds.Contains(t.AsignaturaId))
            .OrderBy(t => t.Trimestre)
            .ThenBy(t => t.Nombre)
            .Select(t => new ProfesorTareaStatsRow(t.Id, t.Nombre, t.Trimestre, t.AsignaturaId, t.ProfesorId))
            .ToListAsync(cancellationToken);

        var taskIds = tasks.Select(t => t.TareaId).ToList();
        var studentIds = students.Select(student => student.EstudianteId).Distinct().ToList();
        var gradeMap = await context.Notas
            .AsNoTracking()
            .Where(n => taskIds.Contains(n.TareaId) && studentIds.Contains(n.EstudianteId))
            .ToDictionaryAsync(n => (n.EstudianteId, n.TareaId), n => (decimal?)n.Valor, cancellationToken);

        var globalAverages = new List<double>();
        var subjectStats = new List<AsignaturaStatsProfesorDto>();

        foreach (var assignment in assignments)
        {
            var subjectStudents = students
                .Where(student => student.AsignaturaId == assignment.AsignaturaId)
                .ToList();

            var subjectTasks = tasks
                .Where(t => t.AsignaturaId == assignment.AsignaturaId)
                .Select(t => new TareaResumenDto { TareaId = t.TareaId, Nombre = t.Nombre, Trimestre = t.Trimestre })
                .ToList();

            var teacherTasks = tasks
                .Where(t => t.AsignaturaId == assignment.AsignaturaId && t.ProfesorId == profesorId)
                .ToList();

            var studentSummaries = subjectStudents
                .Select(student => BuildAlumnoResumen(student.EstudianteId, student.Alumno, subjectTasks, gradeMap))
                .ToList();

            var finals = studentSummaries
                .Where(alumnoResumen => alumnoResumen.NotaFinal.HasValue)
                .Select(alumnoResumen => (double)alumnoResumen.NotaFinal!.Value)
                .ToList();

            globalAverages.AddRange(finals);

            subjectStats.Add(new AsignaturaStatsProfesorDto
            {
                AsignaturaId = assignment.AsignaturaId,
                Asignatura = assignment.Asignatura,
                Curso = assignment.Curso,
                TotalAlumnos = studentSummaries.Count,
                Aprobados = studentSummaries.Count(alumnoResumen => alumnoResumen.NotaFinal.HasValue && alumnoResumen.NotaFinal.Value >= 5),
                Suspensos = studentSummaries.Count(alumnoResumen => alumnoResumen.NotaFinal.HasValue && alumnoResumen.NotaFinal.Value < 5),
                SinNota = studentSummaries.Count(alumnoResumen => !alumnoResumen.NotaFinal.HasValue),
                Media = finals.Count > 0 ? Math.Round(finals.Average(), 2) : null,
                PorTarea = teacherTasks
                    .Select(task => BuildTareaStats(task, subjectStudents, gradeMap))
                    .ToList()
            });
        }

        return new ProfesorStatsDto
        {
            ProfesorId = teacher.Id,
            Nombre = teacher.Nombre,
            MediaGlobal = globalAverages.Count > 0 ? Math.Round(globalAverages.Average(), 2) : null,
            Asignaturas = subjectStats
        };
    }

    private static AsignaturaAlumnoResumenDto BuildAlumnoResumen(
        int estudianteId,
        string student,
        IEnumerable<TareaResumenDto> tasks,
        IReadOnlyDictionary<(int EstudianteId, int TareaId), decimal?> gradeMap)
    {
        var tasksList = tasks.ToList();

        decimal? MediaTrimestre(int trimestre)
        {
            var values = tasksList
                .Where(task => task.Trimestre == trimestre)
                .Select(task => gradeMap.TryGetValue((estudianteId, task.TareaId), out var value) ? value : null)
                .Where(value => value.HasValue)
                .Select(value => value!.Value)
                .ToList();

            return values.Count > 0 ? Math.Round(values.Average(), 2) : null;
        }

        var t1 = MediaTrimestre(1);
        var t2 = MediaTrimestre(2);
        var t3 = MediaTrimestre(3);

        return new AsignaturaAlumnoResumenDto
        {
            EstudianteId = estudianteId,
            Alumno = student,
            Medias = new MediasTrimestralesDto
            {
                T1 = t1,
                T2 = t2,
                T3 = t3
            },
            NotaFinal = (t1.HasValue && t2.HasValue && t3.HasValue)
                ? Math.Round((t1.Value + t2.Value + t3.Value) / 3, 2)
                : null
        };
    }

    private static TareaStatsDto BuildTareaStats(
        ProfesorTareaStatsRow task,
        IEnumerable<ProfesorAlumnoStatsRow> students,
        IReadOnlyDictionary<(int EstudianteId, int TareaId), decimal?> gradeMap)
    {
        var studentsList = students.ToList();
        var values = studentsList
            .Select(student => gradeMap.TryGetValue((student.EstudianteId, task.TareaId), out var value) ? value : null)
            .Where(value => value.HasValue)
            .Select(value => (double)value!.Value)
            .ToList();

        return new TareaStatsDto
        {
            TareaId = task.TareaId,
            Nombre = task.Nombre,
            Trimestre = task.Trimestre,
            Media = values.Count > 0 ? Math.Round(values.Average(), 2) : null,
            TotalNotas = values.Count,
            SinNota = studentsList.Count - values.Count,
            NotaMax = values.Count > 0 ? values.Max() : null,
            NotaMin = values.Count > 0 ? values.Min() : null
        };
    }

    #endregion

    #region Mutations

    public async Task<ProfesorListItemDto> CreateProfesorAsync(string nombre, string correo, string contrasenaHash, string apellidos, string dni, string telefono, string especialidad, CancellationToken cancellationToken = default)
    {
        var teacher = await context.Profesores
            .IgnoreQueryFilters()
            .Include(p => p.Cuenta)
            .FirstOrDefaultAsync(p => p.Cuenta != null && p.Cuenta.Correo == correo && p.Cuenta.ColegioId == currentSchoolContext.SchoolId, cancellationToken);

        if (teacher is null)
        {
            teacher = new Profesor
            {
                Nombre = nombre,
                Apellidos = apellidos,
                DNI = dni,
                Telefono = telefono,
                Especialidad = especialidad,
                Cuenta = new Cuenta
                {
                    Correo = correo,
                    Contrasena = contrasenaHash,
                    Rol = Roles.Profesor,
                    ColegioId = currentSchoolContext.SchoolId
                }
            };
            context.Profesores.Add(teacher);
        }
        else
        {
            teacher.Nombre = nombre;
            teacher.Apellidos = apellidos;
            teacher.DNI = dni;
            teacher.Telefono = telefono;
            teacher.Especialidad = especialidad;
            teacher.IsDeleted = false;
            if (teacher.Cuenta is not null)
            {
                teacher.Cuenta.Correo = correo;
                teacher.Cuenta.Contrasena = contrasenaHash;
                teacher.Cuenta.Rol = Roles.Profesor;
                teacher.Cuenta.ColegioId = currentSchoolContext.SchoolId;
                teacher.Cuenta.IsDeleted = false;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return new ProfesorListItemDto { Id = teacher.Id, Nombre = teacher.Nombre, Apellidos = teacher.Apellidos, DNI = teacher.DNI, Telefono = teacher.Telefono, Especialidad = teacher.Especialidad, Correo = teacher.Cuenta!.Correo, Imparticiones = new() };
    }

    public async Task<ProfesorListItemDto?> UpdateProfesorAsync(int profesorId, string nombre, string correo, string? contrasenaHash, string apellidos, string dni, string telefono, string especialidad, CancellationToken cancellationToken = default)
    {
        var teacher = await context.Profesores
            .Include(p => p.Cuenta)
            .FirstOrDefaultAsync(p => p.Id == profesorId, cancellationToken);
        if (teacher is null) return null;

        teacher.Nombre = nombre;
        teacher.Apellidos = apellidos;
        teacher.DNI = dni;
        teacher.Telefono = telefono;
        teacher.Especialidad = especialidad;
        if (teacher.Cuenta is not null)
        {
            teacher.Cuenta.Correo = correo;
            if (contrasenaHash is not null) teacher.Cuenta.Contrasena = contrasenaHash;
        }

        await context.SaveChangesAsync(cancellationToken);

        var assignments = await context.ProfesorAsignaturaCursos
            .AsNoTracking()
            .Where(i => i.ProfesorId == profesorId)
            .Select(i => new ProfesorImparticionDto
            {
                AsignaturaId = i.AsignaturaId,
                Asignatura = i.Asignatura!.Nombre,
                CursoId = i.CursoId,
                Curso = i.Curso!.Nombre
            })
            .ToListAsync(cancellationToken);

        return new ProfesorListItemDto { Id = teacher.Id, Nombre = teacher.Nombre, Apellidos = teacher.Apellidos, DNI = teacher.DNI, Telefono = teacher.Telefono, Especialidad = teacher.Especialidad, Correo = teacher.Cuenta!.Correo, Imparticiones = assignments };
    }

    public async Task DeleteProfesorAsync(int profesorId, CancellationToken cancellationToken = default)
    {
        var assignments = await context.ProfesorAsignaturaCursos
            .Where(i => i.ProfesorId == profesorId).ToListAsync(cancellationToken);
        foreach (var assignment in assignments) assignment.IsDeleted = true;

        var taskIds = await context.Tareas
            .Where(t => t.ProfesorId == profesorId).Select(t => t.Id).ToListAsync(cancellationToken);
        if (taskIds.Count > 0)
        {
            var grades = await context.Notas.Where(n => taskIds.Contains(n.TareaId)).ToListAsync(cancellationToken);
            foreach (var grade in grades) grade.IsDeleted = true;
            var tasks = await context.Tareas.Where(t => t.ProfesorId == profesorId).ToListAsync(cancellationToken);
            foreach (var task in tasks) task.IsDeleted = true;
        }

        var tokens = await context.RefreshTokens
            .Where(t => t.UserId == profesorId && t.Rol == "teacher")
            .ToListAsync(cancellationToken);
        foreach (var token in tokens) token.IsDeleted = true;

        var teacher = await context.Profesores
            .Include(p => p.Cuenta)
            .FirstOrDefaultAsync(p => p.Id == profesorId, cancellationToken);
        if (teacher is not null)
        {
            teacher.IsDeleted = true;
            if (teacher.Cuenta is not null)
                teacher.Cuenta.IsDeleted = true;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AsignarImparticionAsync(int profesorId, int subjectId, int cursoId, CancellationToken cancellationToken = default)
    {
        var record = await context.ProfesorAsignaturaCursos
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.ProfesorId == profesorId && i.AsignaturaId == subjectId && i.CursoId == cursoId, cancellationToken);

        if (record is null)
        {
            context.ProfesorAsignaturaCursos.Add(new ProfesorAsignaturaCurso
            {
                ProfesorId = profesorId,
                AsignaturaId = subjectId,
                CursoId = cursoId
            });
        }
        else
        {
            record.IsDeleted = false;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task EliminarImparticionAsync(int profesorId, int subjectId, int cursoId, CancellationToken cancellationToken = default)
    {
        var record = await context.ProfesorAsignaturaCursos
            .FirstOrDefaultAsync(i => i.ProfesorId == profesorId && i.AsignaturaId == subjectId && i.CursoId == cursoId, cancellationToken);
        if (record is not null)
        {
            record.IsDeleted = true;
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task SetNotaAsync(int estudianteId, int tareaId, decimal value, CancellationToken cancellationToken = default)
    {
        var grade = await context.Notas
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(n => n.EstudianteId == estudianteId && n.TareaId == tareaId, cancellationToken);

        if (grade is null)
            context.Notas.Add(new Nota { EstudianteId = estudianteId, TareaId = tareaId, Valor = value });
        else
        {
            grade.Valor = value;
            grade.IsDeleted = false;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<TareaDetalleDto> CrearTareaAsync(string nombre, int trimestre, int subjectId, int profesorId, CancellationToken cancellationToken = default)
    {
        var subjectName = await context.Asignaturas
            .AsNoTracking()
            .Where(a => a.Id == subjectId)
            .Select(a => a.Nombre)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var task = new Tarea { Nombre = nombre, Trimestre = trimestre, AsignaturaId = subjectId, ProfesorId = profesorId };
        context.Tareas.Add(task);
        await context.SaveChangesAsync(cancellationToken);

        return new TareaDetalleDto { Id = task.Id, Nombre = task.Nombre, Trimestre = task.Trimestre, AsignaturaId = task.AsignaturaId, Asignatura = subjectName };
    }

    #endregion
}


