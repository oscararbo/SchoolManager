using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Persistence.Context;
using Back.Api.Application.Dtos;
using Back.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Back.Api.Persistence.Repositories;

public class ProfesoresDomainRepository(AppDbContext context) : IProfesoresDomainRepository
{
    private sealed record ProfesorAlumnoStatsRow(int EstudianteId, string Alumno, int AsignaturaId);
    private sealed record ProfesorTareaStatsRow(int TareaId, string Nombre, int Trimestre, int AsignaturaId, int ProfesorId);

    #region Checks

    public Task<bool> ProfesorExisteAsync(int profesorId, CancellationToken cancellationToken = default)
        => context.Profesores.AnyAsync(p => p.Id == profesorId);

    public Task<bool> ProfesorImparteAsignaturaAsync(int profesorId, int asignaturaId, CancellationToken cancellationToken = default)
        => context.ProfesorAsignaturaCursos
            .AnyAsync(pac => pac.ProfesorId == profesorId && pac.AsignaturaId == asignaturaId);

    public Task<bool> ProfesorImparteTareaAsync(int profesorId, int tareaId, CancellationToken cancellationToken = default)
        => context.Tareas.AnyAsync(t => t.Id == tareaId && t.ProfesorId == profesorId);

    public Task<bool> CorreoDuplicadoAsync(string correo, CancellationToken cancellationToken = default)
        => context.Cuentas.AnyAsync(c => c.Correo == correo);

    public Task<bool> CorreoDuplicadoExceptAsync(string correo, int exceptProfesorId, CancellationToken cancellationToken = default)
        => context.Cuentas.AnyAsync(c => c.Correo == correo && (c.Profesor == null || c.Profesor.Id != exceptProfesorId));

    public Task<bool> CursoExisteAsync(int cursoId, CancellationToken cancellationToken = default)
        => context.Cursos.AnyAsync(c => c.Id == cursoId);

    public Task<bool> AsignaturaYaTieneOtroProfesorAsync(int asignaturaId, int profesorId, CancellationToken cancellationToken = default)
        => context.ProfesorAsignaturaCursos
            .AnyAsync(imparticion => imparticion.AsignaturaId == asignaturaId && imparticion.ProfesorId != profesorId);

    public Task<bool> ImparticionExisteAsync(int profesorId, int asignaturaId, int cursoId, CancellationToken cancellationToken = default)
        => context.ProfesorAsignaturaCursos
            .AnyAsync(imparticion => imparticion.ProfesorId == profesorId && imparticion.AsignaturaId == asignaturaId && imparticion.CursoId == cursoId);

    public Task<bool> EstudianteMatriculadoAsync(int estudianteId, int asignaturaId, CancellationToken cancellationToken = default)
        => context.EstudianteAsignaturas
            .AnyAsync(matricula => matricula.EstudianteId == estudianteId && matricula.AsignaturaId == asignaturaId);

    public Task<bool> ProfesorImparteAlCursoAsync(int profesorId, int asignaturaId, int cursoId, CancellationToken cancellationToken = default)
        => context.ProfesorAsignaturaCursos
            .AnyAsync(imparticion => imparticion.ProfesorId == profesorId && imparticion.AsignaturaId == asignaturaId && imparticion.CursoId == cursoId);

    public Task<bool> TareaDuplicadaAsync(int asignaturaId, int trimestre, string nombre, CancellationToken cancellationToken = default)
        => context.Tareas
            .AnyAsync(t => t.AsignaturaId == asignaturaId && t.Trimestre == trimestre && t.Nombre == nombre);

    #endregion

    #region Simple lookups

    public async Task<AsignaturaInfoDto?> GetAsignaturaInfoAsync(int asignaturaId, CancellationToken cancellationToken = default)
        => await context.Asignaturas
            .AsNoTracking()
            .Where(a => a.Id == asignaturaId)
            .Select(a => new AsignaturaInfoDto { Id = a.Id, Nombre = a.Nombre, CursoId = a.CursoId, Curso = a.Curso!.Nombre })
            .FirstOrDefaultAsync();

    public async Task<TareaResumenDto?> GetTareaResumenAsync(int tareaId, CancellationToken cancellationToken = default)
        => await context.Tareas
            .AsNoTracking()
            .Where(t => t.Id == tareaId)
            .Select(t => new TareaResumenDto { TareaId = t.Id, Nombre = t.Nombre, Trimestre = t.Trimestre })
            .FirstOrDefaultAsync();

    public async Task<(int Id, int CursoId)?> GetAsignaturaBasicaAsync(int asignaturaId, CancellationToken cancellationToken = default)
    {
        var asignatura = await context.Asignaturas
            .AsNoTracking()
            .Where(a => a.Id == asignaturaId)
            .Select(a => new { a.Id, a.CursoId })
            .FirstOrDefaultAsync();

        return asignatura is null ? null : (asignatura.Id, asignatura.CursoId);
    }

    public async Task<(int Id, int AsignaturaId, int ProfesorId)?> GetTareaInfoAsync(int tareaId, CancellationToken cancellationToken = default)
    {
        var tarea = await context.Tareas
            .AsNoTracking()
            .Where(t => t.Id == tareaId)
            .Select(t => new { t.Id, t.AsignaturaId, t.ProfesorId })
            .FirstOrDefaultAsync();

        return tarea is null ? null : (tarea.Id, tarea.AsignaturaId, tarea.ProfesorId);
    }

    public async Task<int?> GetEstudianteCursoAsync(int estudianteId, CancellationToken cancellationToken = default)
    {
        var cursoId = await context.Estudiantes
            .AsNoTracking()
            .Where(e => e.Id == estudianteId)
            .Select(e => (int?)e.CursoId)
            .FirstOrDefaultAsync();

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
            .Select(profesor => new ProfesorListItemDto
            {
                Id = profesor.Id,
                Nombre = profesor.Nombre,
                Apellidos = profesor.Apellidos,
                DNI = profesor.DNI,
                Telefono = profesor.Telefono,
                Especialidad = profesor.Especialidad,
                Correo = profesor.Cuenta!.Correo,
                Imparticiones = profesor.ProfesorAsignaturaCursos.Select(imparticion => new ProfesorImparticionDto
                {
                    AsignaturaId = imparticion.AsignaturaId,
                    Asignatura = imparticion.Asignatura!.Nombre,
                    CursoId = imparticion.CursoId,
                    Curso = imparticion.Curso!.Nombre
                }).ToList()
            })
            .ToListAsync(cancellationToken);

    public async Task<ProfesorDetalleDto?> GetDetalleAsync(int profesorId, CancellationToken cancellationToken = default)
    {
        var profesor = await context.Profesores
            .AsNoTracking()
            .Where(p => p.Id == profesorId)
            .Select(p => new { p.Id, p.Nombre, p.Apellidos, p.DNI, p.Telefono, p.Especialidad, Correo = p.Cuenta!.Correo })
            .FirstOrDefaultAsync();

        if (profesor is null) return null;

        var cursos = await context.ProfesorAsignaturaCursos
            .AsNoTracking()
            .Where(i => i.ProfesorId == profesorId)
            .OrderBy(i => i.Curso!.Nombre).ThenBy(i => i.Asignatura!.Nombre)
            .Select(i => new { CursoId = i.CursoId, Curso = i.Curso!.Nombre, AsignaturaId = i.AsignaturaId, Asignatura = i.Asignatura!.Nombre })
            .ToListAsync();

        return new ProfesorDetalleDto
        {
            Id = profesor.Id,
            Nombre = profesor.Nombre,
            Apellidos = profesor.Apellidos,
            DNI = profesor.DNI,
            Telefono = profesor.Telefono,
            Especialidad = profesor.Especialidad,
            Correo = profesor.Correo,
            Cursos = cursos
                .GroupBy(cursoAsignatura => new { cursoAsignatura.CursoId, cursoAsignatura.Curso })
                .Select(grupoCurso => new ProfesorCursoPanelDto
                {
                    CursoId = grupoCurso.Key.CursoId,
                    Curso = grupoCurso.Key.Curso,
                    Asignaturas = grupoCurso.Select(asignatura => new ProfesorCursoAsignaturaDto { AsignaturaId = asignatura.AsignaturaId, Nombre = asignatura.Asignatura }).ToList()
                })
                .OrderBy(curso => curso.Curso)
                .ToList()
        };
    }

    public async Task<ProfesorPanelDto?> GetPanelAsync(int profesorId, CancellationToken cancellationToken = default)
    {
        var profesor = await context.Profesores
            .AsNoTracking()
            .Where(p => p.Id == profesorId)
            .Select(p => new { p.Id, p.Nombre })
            .FirstOrDefaultAsync();

        if (profesor is null) return null;

        var cursos = await context.ProfesorAsignaturaCursos
            .AsNoTracking()
            .Where(i => i.ProfesorId == profesorId)
            .OrderBy(i => i.Curso!.Nombre).ThenBy(i => i.Asignatura!.Nombre)
            .Select(i => new { CursoId = i.CursoId, Curso = i.Curso!.Nombre, AsignaturaId = i.AsignaturaId, Asignatura = i.Asignatura!.Nombre })
            .ToListAsync();

        return new ProfesorPanelDto
        {
            Id = profesor.Id,
            Nombre = profesor.Nombre,
            Cursos = cursos
                .GroupBy(cursoAsignatura => new { cursoAsignatura.CursoId, cursoAsignatura.Curso })
                .Select(grupoCurso => new ProfesorCursoPanelDto
                {
                    CursoId = grupoCurso.Key.CursoId,
                    Curso = grupoCurso.Key.Curso,
                    Asignaturas = grupoCurso.Select(asignatura => new ProfesorCursoAsignaturaDto { AsignaturaId = asignatura.AsignaturaId, Nombre = asignatura.Asignatura }).ToList()
                })
                .OrderBy(curso => curso.Curso)
                .ToList()
        };
    }

    public async Task<AsignaturaAlumnosResponseDto?> GetAlumnosCompletoAsync(int asignaturaId, CancellationToken cancellationToken = default)
    {
        var asignaturaInfo = await GetAsignaturaInfoAsync(asignaturaId);
        if (asignaturaInfo is null) return null;

        var tareas = await GetTareasDeAsignaturaAsync(asignaturaId, cancellationToken);
        var tareaIds = tareas.Select(t => t.TareaId).ToList();

        var alumnosRaw = await context.EstudianteAsignaturas
            .AsNoTracking()
            .Where(ea => ea.AsignaturaId == asignaturaId)
            .OrderBy(ea => ea.Estudiante!.Nombre)
            .Select(ea => new { ea.EstudianteId, Alumno = ea.Estudiante!.Nombre })
            .ToListAsync();

        var alumnoIds = alumnosRaw.Select(alumnoRegistro => alumnoRegistro.EstudianteId).ToList();
        var todasNotas = await context.Notas
            .AsNoTracking()
            .Where(nota => alumnoIds.Contains(nota.EstudianteId) && tareaIds.Contains(nota.TareaId))
            .ToListAsync();

        var alumnos = alumnosRaw.Select(alumnoRegistro =>
        {
            var notasAlumno = todasNotas.Where(nota => nota.EstudianteId == alumnoRegistro.EstudianteId).ToList();
            var notasList = tareas.Select(tarea => new AsignaturaNotaAlumnoDto
            {
                TareaId = tarea.TareaId,
                Valor = notasAlumno.FirstOrDefault(nota => nota.TareaId == tarea.TareaId)?.Valor
            }).ToList();

            decimal? Media(int trim)
            {
                var valores = tareas.Where(tarea => tarea.Trimestre == trim)
                    .Select(tarea => notasList.First(nota => nota.TareaId == tarea.TareaId).Valor)
                    .Where(valor => valor.HasValue).Select(valor => valor!.Value).ToList();
                return valores.Count > 0 ? valores.Average() : null;
            }

            var t1 = Media(1); var t2 = Media(2); var t3 = Media(3);
            decimal? notaFinal = (t1.HasValue && t2.HasValue && t3.HasValue)
                ? Math.Round((t1.Value + t2.Value + t3.Value) / 3, 2) : null;

            return new AsignaturaAlumnoDto
            {
                EstudianteId = alumnoRegistro.EstudianteId,
                Alumno = alumnoRegistro.Alumno,
                Notas = notasList,
                Medias = new MediasTrimestralesDto
                {
                    T1 = t1.HasValue ? Math.Round(t1.Value, 2) : null,
                    T2 = t2.HasValue ? Math.Round(t2.Value, 2) : null,
                    T3 = t3.HasValue ? Math.Round(t3.Value, 2) : null
                },
                NotaFinal = notaFinal
            };
        }).ToList();

        return new AsignaturaAlumnosResponseDto { Asignatura = asignaturaInfo, Tareas = tareas, Alumnos = alumnos };
    }

    public async Task<List<TareaResumenDto>> GetTareasDeAsignaturaAsync(int asignaturaId, CancellationToken cancellationToken = default)
        => await context.Tareas
            .AsNoTracking()
            .Where(t => t.AsignaturaId == asignaturaId)
            .OrderBy(t => t.Trimestre).ThenBy(t => t.Nombre)
            .Select(t => new TareaResumenDto { TareaId = t.Id, Nombre = t.Nombre, Trimestre = t.Trimestre })
            .ToListAsync(cancellationToken);

    public async Task<List<ProfesorAlumnoResumenRow>> GetAlumnosResumenAsync(int asignaturaId, CancellationToken cancellationToken = default)
        => await context.EstudianteAsignaturas
            .AsNoTracking()
            .Where(ea => ea.AsignaturaId == asignaturaId)
            .OrderBy(ea => ea.Estudiante!.Nombre)
            .Select(ea => new ProfesorAlumnoResumenRow(ea.EstudianteId, ea.Estudiante!.Nombre))
            .ToListAsync(cancellationToken);

    public async Task<AsignaturaAlumnosResumenResponseDto?> GetAlumnosResumenResponseAsync(int asignaturaId, CancellationToken cancellationToken = default)
    {
        var asignaturaInfo = await GetAsignaturaInfoAsync(asignaturaId, cancellationToken);
        if (asignaturaInfo is null) return null;

        var tareas = await GetTareasDeAsignaturaAsync(asignaturaId, cancellationToken);
        var alumnos = await GetAlumnosResumenAsync(asignaturaId, cancellationToken);
        var alumnoIds = alumnos.Select(a => a.EstudianteId).ToList();
        var tareaIds = tareas.Select(t => t.TareaId).ToList();

        var notaMap = await context.Notas
            .AsNoTracking()
            .Where(n => alumnoIds.Contains(n.EstudianteId) && tareaIds.Contains(n.TareaId))
            .ToDictionaryAsync(n => (n.EstudianteId, n.TareaId), n => (decimal?)n.Valor, cancellationToken);

        return new AsignaturaAlumnosResumenResponseDto
        {
            Asignatura = asignaturaInfo,
            Tareas = tareas,
            Alumnos = alumnos
                .Select(alumno => BuildAlumnoResumen(alumno.EstudianteId, alumno.Alumno, tareas, notaMap))
                .ToList()
        };
    }

    public async Task<List<ProfesorTareaCalificacionRow>> GetCalificacionesTareaAsync(int asignaturaId, int tareaId, CancellationToken cancellationToken = default)
    {
        var alumnos = await GetAlumnosResumenAsync(asignaturaId, cancellationToken);
        var notas = await context.Notas
            .AsNoTracking()
            .Where(n => n.TareaId == tareaId)
            .ToDictionaryAsync(n => n.EstudianteId, n => (decimal?)n.Valor, cancellationToken);
        return alumnos.Select(alumnoResumen => new ProfesorTareaCalificacionRow(
            alumnoResumen.EstudianteId, alumnoResumen.Alumno,
            notas.TryGetValue(alumnoResumen.EstudianteId, out var notaValor) ? notaValor : null
        )).ToList();
    }

    public async Task<AsignaturaCalificacionesTareaResponseDto?> GetCalificacionesTareaResponseAsync(int asignaturaId, int tareaId, CancellationToken cancellationToken = default)
    {
        var tarea = await GetTareaResumenAsync(tareaId, cancellationToken);
        if (tarea is null)
            return null;

        var calificaciones = await GetCalificacionesTareaAsync(asignaturaId, tareaId, cancellationToken);
        return new AsignaturaCalificacionesTareaResponseDto
        {
            TareaId = tarea.TareaId,
            Tarea = tarea.Nombre,
            Trimestre = tarea.Trimestre,
            Calificaciones = calificaciones.Select(c => new AsignaturaCalificacionTareaDto
            {
                EstudianteId = c.EstudianteId,
                Alumno = c.Alumno,
                Valor = c.Valor
            }).ToList()
        };
    }

    public async Task<ProfesorAlumnoDetalleDto?> GetAlumnoDetalleAsync(int asignaturaId, int estudianteId, CancellationToken cancellationToken = default)
    {
        var alumno = await context.Estudiantes
            .AsNoTracking()
            .Where(e => e.Id == estudianteId)
            .Select(e => new { e.Id, e.Nombre })
            .FirstOrDefaultAsync();

        if (alumno is null) return null;

        var isMatriculado = await context.EstudianteAsignaturas
            .AnyAsync(ea => ea.EstudianteId == estudianteId && ea.AsignaturaId == asignaturaId);
        if (!isMatriculado) return null;

        var tareas = await GetTareasDeAsignaturaAsync(asignaturaId, cancellationToken);
        var tareaIds = tareas.Select(t => t.TareaId).ToList();
        var notas = await context.Notas
            .AsNoTracking()
            .Where(n => n.EstudianteId == estudianteId && tareaIds.Contains(n.TareaId))
            .ToListAsync();

        var notasList = tareas.Select(tarea => new AsignaturaNotaAlumnoDto
        {
            TareaId = tarea.TareaId,
            Valor = notas.FirstOrDefault(nota => nota.TareaId == tarea.TareaId)?.Valor
        }).ToList();

        decimal? Media(int trim)
        {
            var valores = tareas.Where(tarea => tarea.Trimestre == trim)
                .Select(tarea => notasList.First(nota => nota.TareaId == tarea.TareaId).Valor)
                .Where(valor => valor.HasValue).Select(valor => valor!.Value).ToList();
            return valores.Count > 0 ? Math.Round(valores.Average(), 2) : null;
        }

        var t1 = Media(1); var t2 = Media(2); var t3 = Media(3);
        return new ProfesorAlumnoDetalleDto
        {
            EstudianteId = alumno.Id,
            Alumno = alumno.Nombre,
            Notas = notasList,
            Medias = new MediasTrimestralesDto { T1 = t1, T2 = t2, T3 = t3 },
            NotaFinal = (t1.HasValue && t2.HasValue && t3.HasValue)
                ? Math.Round((t1.Value + t2.Value + t3.Value) / 3, 2) : null
        };
    }

    public async Task<IEnumerable<TareaResumenDto>> GetTareasDeProfesorEnAsignaturaAsync(int profesorId, int asignaturaId, CancellationToken cancellationToken = default)
        => await context.Tareas
            .AsNoTracking()
            .Where(t => t.AsignaturaId == asignaturaId && t.ProfesorId == profesorId)
            .OrderBy(t => t.Trimestre).ThenBy(t => t.Nombre)
            .Select(t => new TareaResumenDto { TareaId = t.Id, Nombre = t.Nombre, Trimestre = t.Trimestre })
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<TareaConNotasDto>> GetTareasConNotasAsync(int asignaturaId, CancellationToken cancellationToken = default)
    {
        var asignaturaNombre = await context.Asignaturas
            .AsNoTracking()
            .Where(a => a.Id == asignaturaId)
            .Select(a => a.Nombre)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var tareas = await context.Tareas
            .AsNoTracking()
            .Where(t => t.AsignaturaId == asignaturaId)
            .OrderBy(t => t.Trimestre).ThenBy(t => t.Nombre)
            .ToListAsync(cancellationToken);

        var tareaIds = tareas.Select(t => t.Id).ToList();
        var notas = await context.Notas
            .AsNoTracking()
            .Where(n => tareaIds.Contains(n.TareaId))
            .Select(nota => new { nota.TareaId, nota.EstudianteId, Alumno = nota.Estudiante!.Nombre, nota.Valor })
            .ToListAsync(cancellationToken);

        return tareas.Select(tarea => new TareaConNotasDto
        {
            TareaId = tarea.Id,
            Nombre = tarea.Nombre,
            Trimestre = tarea.Trimestre,
            AsignaturaId = tarea.AsignaturaId,
            Asignatura = asignaturaNombre,
            Notas = notas
                .Where(nota => nota.TareaId == tarea.Id)
                .OrderBy(nota => nota.Alumno)
                .Select(nota => new TareaNotaAlumnoDto { EstudianteId = nota.EstudianteId, Alumno = nota.Alumno, Valor = nota.Valor })
                .ToList()
        });
    }

    public async Task<ProfesorStatsDto?> GetStatsAsync(int profesorId, CancellationToken cancellationToken = default)
    {
        var profesor = await context.Profesores
            .AsNoTracking()
            .Where(p => p.Id == profesorId)
            .Select(p => new { p.Id, p.Nombre })
            .FirstOrDefaultAsync(cancellationToken);

        if (profesor is null)
            return null;

        var asignaciones = await context.ProfesorAsignaturaCursos
            .AsNoTracking()
            .Where(asignacion => asignacion.ProfesorId == profesorId)
            .OrderBy(asignacion => asignacion.Curso!.Nombre)
            .ThenBy(asignacion => asignacion.Asignatura!.Nombre)
            .Select(asignacion => new { asignacion.AsignaturaId, Asignatura = asignacion.Asignatura!.Nombre, Curso = asignacion.Curso!.Nombre })
            .ToListAsync(cancellationToken);

        var asignaturaIds = asignaciones.Select(asignacion => asignacion.AsignaturaId).Distinct().ToList();
        if (asignaturaIds.Count == 0)
        {
            return new ProfesorStatsDto
            {
                ProfesorId = profesor.Id,
                Nombre = profesor.Nombre,
                MediaGlobal = null,
                Asignaturas = []
            };
        }

        var alumnos = await context.EstudianteAsignaturas
            .AsNoTracking()
            .Where(ea => asignaturaIds.Contains(ea.AsignaturaId))
            .OrderBy(ea => ea.Estudiante!.Nombre)
            .Select(ea => new ProfesorAlumnoStatsRow(ea.EstudianteId, ea.Estudiante!.Nombre, ea.AsignaturaId))
            .ToListAsync(cancellationToken);

        var tareas = await context.Tareas
            .AsNoTracking()
            .Where(t => asignaturaIds.Contains(t.AsignaturaId))
            .OrderBy(t => t.Trimestre)
            .ThenBy(t => t.Nombre)
            .Select(t => new ProfesorTareaStatsRow(t.Id, t.Nombre, t.Trimestre, t.AsignaturaId, t.ProfesorId))
            .ToListAsync(cancellationToken);

        var tareaIds = tareas.Select(t => t.TareaId).ToList();
        var alumnoIds = alumnos.Select(alumno => alumno.EstudianteId).Distinct().ToList();
        var notaMap = await context.Notas
            .AsNoTracking()
            .Where(n => tareaIds.Contains(n.TareaId) && alumnoIds.Contains(n.EstudianteId))
            .ToDictionaryAsync(n => (n.EstudianteId, n.TareaId), n => (decimal?)n.Valor, cancellationToken);

        var mediasGlobales = new List<double>();
        var asignaturasStats = new List<AsignaturaStatsProfesorDto>();

        foreach (var asignacion in asignaciones)
        {
            var alumnosAsignatura = alumnos
                .Where(alumno => alumno.AsignaturaId == asignacion.AsignaturaId)
                .ToList();

            var tareasAsignatura = tareas
                .Where(t => t.AsignaturaId == asignacion.AsignaturaId)
                .Select(t => new TareaResumenDto { TareaId = t.TareaId, Nombre = t.Nombre, Trimestre = t.Trimestre })
                .ToList();

            var tareasProfesor = tareas
                .Where(t => t.AsignaturaId == asignacion.AsignaturaId && t.ProfesorId == profesorId)
                .ToList();

            var alumnosResumen = alumnosAsignatura
                .Select(alumno => BuildAlumnoResumen(alumno.EstudianteId, alumno.Alumno, tareasAsignatura, notaMap))
                .ToList();

            var finales = alumnosResumen
                .Where(alumnoResumen => alumnoResumen.NotaFinal.HasValue)
                .Select(alumnoResumen => (double)alumnoResumen.NotaFinal!.Value)
                .ToList();

            mediasGlobales.AddRange(finales);

            asignaturasStats.Add(new AsignaturaStatsProfesorDto
            {
                AsignaturaId = asignacion.AsignaturaId,
                Asignatura = asignacion.Asignatura,
                Curso = asignacion.Curso,
                TotalAlumnos = alumnosResumen.Count,
                Aprobados = alumnosResumen.Count(alumnoResumen => alumnoResumen.NotaFinal.HasValue && alumnoResumen.NotaFinal.Value >= 5),
                Suspensos = alumnosResumen.Count(alumnoResumen => alumnoResumen.NotaFinal.HasValue && alumnoResumen.NotaFinal.Value < 5),
                SinNota = alumnosResumen.Count(alumnoResumen => !alumnoResumen.NotaFinal.HasValue),
                Media = finales.Count > 0 ? Math.Round(finales.Average(), 2) : null,
                PorTarea = tareasProfesor
                    .Select(tarea => BuildTareaStats(tarea, alumnosAsignatura, notaMap))
                    .ToList()
            });
        }

        return new ProfesorStatsDto
        {
            ProfesorId = profesor.Id,
            Nombre = profesor.Nombre,
            MediaGlobal = mediasGlobales.Count > 0 ? Math.Round(mediasGlobales.Average(), 2) : null,
            Asignaturas = asignaturasStats
        };
    }

    private static AsignaturaAlumnoResumenDto BuildAlumnoResumen(
        int estudianteId,
        string alumno,
        IEnumerable<TareaResumenDto> tareas,
        IReadOnlyDictionary<(int EstudianteId, int TareaId), decimal?> notaMap)
    {
        var tareasList = tareas.ToList();

        decimal? MediaTrimestre(int trimestre)
        {
            var valores = tareasList
                .Where(tarea => tarea.Trimestre == trimestre)
                .Select(tarea => notaMap.TryGetValue((estudianteId, tarea.TareaId), out var valor) ? valor : null)
                .Where(valor => valor.HasValue)
                .Select(valor => valor!.Value)
                .ToList();

            return valores.Count > 0 ? Math.Round(valores.Average(), 2) : null;
        }

        var t1 = MediaTrimestre(1);
        var t2 = MediaTrimestre(2);
        var t3 = MediaTrimestre(3);

        return new AsignaturaAlumnoResumenDto
        {
            EstudianteId = estudianteId,
            Alumno = alumno,
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
        ProfesorTareaStatsRow tarea,
        IEnumerable<ProfesorAlumnoStatsRow> alumnos,
        IReadOnlyDictionary<(int EstudianteId, int TareaId), decimal?> notaMap)
    {
        var alumnosList = alumnos.ToList();
        var valores = alumnosList
            .Select(alumno => notaMap.TryGetValue((alumno.EstudianteId, tarea.TareaId), out var valor) ? valor : null)
            .Where(valor => valor.HasValue)
            .Select(valor => (double)valor!.Value)
            .ToList();

        return new TareaStatsDto
        {
            TareaId = tarea.TareaId,
            Nombre = tarea.Nombre,
            Trimestre = tarea.Trimestre,
            Media = valores.Count > 0 ? Math.Round(valores.Average(), 2) : null,
            TotalNotas = valores.Count,
            SinNota = alumnosList.Count - valores.Count,
            NotaMax = valores.Count > 0 ? valores.Max() : null,
            NotaMin = valores.Count > 0 ? valores.Min() : null
        };
    }

    #endregion

    #region Mutations

    public async Task<ProfesorListItemDto> CreateProfesorAsync(string nombre, string correo, string contrasenaHash, string apellidos, string dni, string telefono, string especialidad, CancellationToken cancellationToken = default)
    {
        var profesor = await context.Profesores
            .IgnoreQueryFilters()
            .Include(p => p.Cuenta)
            .FirstOrDefaultAsync(p => p.Cuenta != null && p.Cuenta.Correo == correo, cancellationToken);

        if (profesor is null)
        {
            profesor = new Profesor
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
                    Rol = "profesor"
                }
            };
            context.Profesores.Add(profesor);
        }
        else
        {
            profesor.Nombre = nombre;
            profesor.Apellidos = apellidos;
            profesor.DNI = dni;
            profesor.Telefono = telefono;
            profesor.Especialidad = especialidad;
            profesor.IsDeleted = false;
            if (profesor.Cuenta is not null)
            {
                profesor.Cuenta.Correo = correo;
                profesor.Cuenta.Contrasena = contrasenaHash;
                profesor.Cuenta.Rol = "profesor";
                profesor.Cuenta.IsDeleted = false;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return new ProfesorListItemDto { Id = profesor.Id, Nombre = profesor.Nombre, Apellidos = profesor.Apellidos, DNI = profesor.DNI, Telefono = profesor.Telefono, Especialidad = profesor.Especialidad, Correo = profesor.Cuenta!.Correo, Imparticiones = new() };
    }

    public async Task<ProfesorListItemDto?> UpdateProfesorAsync(int profesorId, string nombre, string correo, string? contrasenaHash, string apellidos, string dni, string telefono, string especialidad, CancellationToken cancellationToken = default)
    {
        var profesor = await context.Profesores
            .Include(p => p.Cuenta)
            .FirstOrDefaultAsync(p => p.Id == profesorId, cancellationToken);
        if (profesor is null) return null;

        profesor.Nombre = nombre;
        profesor.Apellidos = apellidos;
        profesor.DNI = dni;
        profesor.Telefono = telefono;
        profesor.Especialidad = especialidad;
        if (profesor.Cuenta is not null)
        {
            profesor.Cuenta.Correo = correo;
            if (contrasenaHash is not null) profesor.Cuenta.Contrasena = contrasenaHash;
        }

        await context.SaveChangesAsync(cancellationToken);

        var imparticiones = await context.ProfesorAsignaturaCursos
            .AsNoTracking()
            .Where(i => i.ProfesorId == profesorId)
            .Select(i => new ProfesorImparticionDto
            {
                AsignaturaId = i.AsignaturaId,
                Asignatura = i.Asignatura!.Nombre,
                CursoId = i.CursoId,
                Curso = i.Curso!.Nombre
            })
            .ToListAsync();

        return new ProfesorListItemDto { Id = profesor.Id, Nombre = profesor.Nombre, Apellidos = profesor.Apellidos, DNI = profesor.DNI, Telefono = profesor.Telefono, Especialidad = profesor.Especialidad, Correo = profesor.Cuenta!.Correo, Imparticiones = imparticiones };
    }

    public async Task DeleteProfesorAsync(int profesorId, CancellationToken cancellationToken = default)
    {
        var imparticiones = await context.ProfesorAsignaturaCursos
            .Where(i => i.ProfesorId == profesorId).ToListAsync(cancellationToken);
        foreach (var imparticion in imparticiones) imparticion.IsDeleted = true;

        var tareaIds = await context.Tareas
            .Where(t => t.ProfesorId == profesorId).Select(t => t.Id).ToListAsync(cancellationToken);
        if (tareaIds.Count > 0)
        {
            var notas = await context.Notas.Where(n => tareaIds.Contains(n.TareaId)).ToListAsync(cancellationToken);
            foreach (var nota in notas) nota.IsDeleted = true;
            var tareas = await context.Tareas.Where(t => t.ProfesorId == profesorId).ToListAsync(cancellationToken);
            foreach (var tarea in tareas) tarea.IsDeleted = true;
        }

        var tokens = await context.RefreshTokens
            .Where(t => t.UserId == profesorId && t.Rol == "profesor")
            .ToListAsync(cancellationToken);
        foreach (var token in tokens) token.IsDeleted = true;

        var profesor = await context.Profesores
            .Include(p => p.Cuenta)
            .FirstOrDefaultAsync(p => p.Id == profesorId, cancellationToken);
        if (profesor is not null)
        {
            profesor.IsDeleted = true;
            if (profesor.Cuenta is not null)
                profesor.Cuenta.IsDeleted = true;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AsignarImparticionAsync(int profesorId, int asignaturaId, int cursoId, CancellationToken cancellationToken = default)
    {
        var registro = await context.ProfesorAsignaturaCursos
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.ProfesorId == profesorId && i.AsignaturaId == asignaturaId && i.CursoId == cursoId, cancellationToken);

        if (registro is null)
        {
            context.ProfesorAsignaturaCursos.Add(new ProfesorAsignaturaCurso
            {
                ProfesorId = profesorId,
                AsignaturaId = asignaturaId,
                CursoId = cursoId
            });
        }
        else
        {
            registro.IsDeleted = false;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task EliminarImparticionAsync(int profesorId, int asignaturaId, int cursoId, CancellationToken cancellationToken = default)
    {
        var registro = await context.ProfesorAsignaturaCursos
            .FirstOrDefaultAsync(i => i.ProfesorId == profesorId && i.AsignaturaId == asignaturaId && i.CursoId == cursoId, cancellationToken);
        if (registro is not null)
        {
            registro.IsDeleted = true;
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task SetNotaAsync(int estudianteId, int tareaId, decimal valor, CancellationToken cancellationToken = default)
    {
        var nota = await context.Notas
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(n => n.EstudianteId == estudianteId && n.TareaId == tareaId, cancellationToken);

        if (nota is null)
            context.Notas.Add(new Nota { EstudianteId = estudianteId, TareaId = tareaId, Valor = valor });
        else
        {
            nota.Valor = valor;
            nota.IsDeleted = false;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<TareaDetalleDto> CrearTareaAsync(string nombre, int trimestre, int asignaturaId, int profesorId, CancellationToken cancellationToken = default)
    {
        var asignaturaNombre = await context.Asignaturas
            .AsNoTracking()
            .Where(a => a.Id == asignaturaId)
            .Select(a => a.Nombre)
            .FirstOrDefaultAsync() ?? string.Empty;

        var tarea = new Tarea { Nombre = nombre, Trimestre = trimestre, AsignaturaId = asignaturaId, ProfesorId = profesorId };
        context.Tareas.Add(tarea);
        await context.SaveChangesAsync(cancellationToken);

        return new TareaDetalleDto { Id = tarea.Id, Nombre = tarea.Nombre, Trimestre = tarea.Trimestre, AsignaturaId = tarea.AsignaturaId, Asignatura = asignaturaNombre };
    }

    #endregion
}


