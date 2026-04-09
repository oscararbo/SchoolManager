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
        => context.Profesores.AnyAsync(p => p.Correo == correo);

    public Task<bool> CorreoDuplicadoExceptAsync(string correo, int exceptId, CancellationToken cancellationToken = default)
        => context.Profesores.AnyAsync(p => p.Correo == correo && p.Id != exceptId);

    public Task<bool> CursoExisteAsync(int cursoId, CancellationToken cancellationToken = default)
        => context.Cursos.AnyAsync(c => c.Id == cursoId);

    public Task<bool> AsignaturaYaTieneOtroProfesorAsync(int asignaturaId, int profesorId, CancellationToken cancellationToken = default)
        => context.ProfesorAsignaturaCursos
            .AnyAsync(x => x.AsignaturaId == asignaturaId && x.ProfesorId != profesorId);

    public Task<bool> ImparticionExisteAsync(int profesorId, int asignaturaId, int cursoId, CancellationToken cancellationToken = default)
        => context.ProfesorAsignaturaCursos
            .AnyAsync(x => x.ProfesorId == profesorId && x.AsignaturaId == asignaturaId && x.CursoId == cursoId);

    public Task<bool> EstudianteMatriculadoAsync(int estudianteId, int asignaturaId, CancellationToken cancellationToken = default)
        => context.EstudianteAsignaturas
            .AnyAsync(x => x.EstudianteId == estudianteId && x.AsignaturaId == asignaturaId);

    public Task<bool> ProfesorImparteAlCursoAsync(int profesorId, int asignaturaId, int cursoId, CancellationToken cancellationToken = default)
        => context.ProfesorAsignaturaCursos
            .AnyAsync(x => x.ProfesorId == profesorId && x.AsignaturaId == asignaturaId && x.CursoId == cursoId);

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
        var result = await context.Estudiantes
            .AsNoTracking()
            .Where(e => e.Id == estudianteId)
            .Select(e => (int?)e.CursoId)
            .FirstOrDefaultAsync();

        return result;
    }

    #endregion

    #region Queries

    public async Task<IEnumerable<ProfesorSimpleDto>> GetSimpleAsync(CancellationToken cancellationToken = default)
        => await context.Profesores
            .AsNoTracking()
            .OrderBy(p => p.Nombre)
            .Select(p => new ProfesorSimpleDto
            {
                Id = p.Id,
                Nombre = p.Nombre
            })
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<ProfesorListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => await context.Profesores
            .AsNoTracking()
            .Select(p => new ProfesorListItemDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Correo = p.Correo,
                Imparticiones = p.ProfesorAsignaturaCursos.Select(i => new ProfesorImparticionDto
                {
                    AsignaturaId = i.AsignaturaId,
                    Asignatura = i.Asignatura!.Nombre,
                    CursoId = i.CursoId,
                    Curso = i.Curso!.Nombre
                }).ToList()
            })
            .ToListAsync(cancellationToken);

    public async Task<ProfesorDetalleDto?> GetDetalleAsync(int id, CancellationToken cancellationToken = default)
    {
        var profesor = await context.Profesores
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new { p.Id, p.Nombre, p.Correo })
            .FirstOrDefaultAsync();

        if (profesor is null) return null;

        var cursos = await context.ProfesorAsignaturaCursos
            .AsNoTracking()
            .Where(i => i.ProfesorId == id)
            .OrderBy(i => i.Curso!.Nombre).ThenBy(i => i.Asignatura!.Nombre)
            .Select(i => new { CursoId = i.CursoId, Curso = i.Curso!.Nombre, AsignaturaId = i.AsignaturaId, Asignatura = i.Asignatura!.Nombre })
            .ToListAsync();

        return new ProfesorDetalleDto
        {
            Id = profesor.Id,
            Nombre = profesor.Nombre,
            Correo = profesor.Correo,
            Cursos = cursos
                .GroupBy(i => new { i.CursoId, i.Curso })
                .Select(g => new ProfesorCursoPanelDto
                {
                    CursoId = g.Key.CursoId,
                    Curso = g.Key.Curso,
                    Asignaturas = g.Select(x => new ProfesorCursoAsignaturaDto { AsignaturaId = x.AsignaturaId, Nombre = x.Asignatura }).ToList()
                })
                .OrderBy(x => x.Curso)
                .ToList()
        };
    }

    public async Task<ProfesorPanelDto?> GetPanelAsync(int id, CancellationToken cancellationToken = default)
    {
        var profesor = await context.Profesores
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new { p.Id, p.Nombre })
            .FirstOrDefaultAsync();

        if (profesor is null) return null;

        var cursos = await context.ProfesorAsignaturaCursos
            .AsNoTracking()
            .Where(i => i.ProfesorId == id)
            .OrderBy(i => i.Curso!.Nombre).ThenBy(i => i.Asignatura!.Nombre)
            .Select(i => new { CursoId = i.CursoId, Curso = i.Curso!.Nombre, AsignaturaId = i.AsignaturaId, Asignatura = i.Asignatura!.Nombre })
            .ToListAsync();

        return new ProfesorPanelDto
        {
            Id = profesor.Id,
            Nombre = profesor.Nombre,
            Cursos = cursos
                .GroupBy(i => new { i.CursoId, i.Curso })
                .Select(g => new ProfesorCursoPanelDto
                {
                    CursoId = g.Key.CursoId,
                    Curso = g.Key.Curso,
                    Asignaturas = g.Select(x => new ProfesorCursoAsignaturaDto { AsignaturaId = x.AsignaturaId, Nombre = x.Asignatura }).ToList()
                })
                .OrderBy(x => x.Curso)
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

        var alumnoIds = alumnosRaw.Select(a => a.EstudianteId).ToList();
        var todasNotas = await context.Notas
            .AsNoTracking()
            .Where(n => alumnoIds.Contains(n.EstudianteId) && tareaIds.Contains(n.TareaId))
            .ToListAsync();

        var alumnos = alumnosRaw.Select(a =>
        {
            var notasAlumno = todasNotas.Where(n => n.EstudianteId == a.EstudianteId).ToList();
            var notasList = tareas.Select(t => new AsignaturaNotaAlumnoDto
            {
                TareaId = t.TareaId,
                Valor = notasAlumno.FirstOrDefault(n => n.TareaId == t.TareaId)?.Valor
            }).ToList();

            decimal? Media(int trim)
            {
                var vals = tareas.Where(t => t.Trimestre == trim)
                    .Select(t => notasList.First(n => n.TareaId == t.TareaId).Valor)
                    .Where(v => v.HasValue).Select(v => v!.Value).ToList();
                return vals.Count > 0 ? vals.Average() : null;
            }

            var t1 = Media(1); var t2 = Media(2); var t3 = Media(3);
            decimal? notaFinal = (t1.HasValue && t2.HasValue && t3.HasValue)
                ? Math.Round((t1.Value + t2.Value + t3.Value) / 3, 2) : null;

            return new AsignaturaAlumnoDto
            {
                EstudianteId = a.EstudianteId,
                Alumno = a.Alumno,
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
        return alumnos.Select(a => new ProfesorTareaCalificacionRow(
            a.EstudianteId, a.Alumno,
            notas.TryGetValue(a.EstudianteId, out var v) ? v : null
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

        var matriculado = await context.EstudianteAsignaturas
            .AnyAsync(ea => ea.EstudianteId == estudianteId && ea.AsignaturaId == asignaturaId);
        if (!matriculado) return null;

        var tareas = await GetTareasDeAsignaturaAsync(asignaturaId, cancellationToken);
        var tareaIds = tareas.Select(t => t.TareaId).ToList();
        var notas = await context.Notas
            .AsNoTracking()
            .Where(n => n.EstudianteId == estudianteId && tareaIds.Contains(n.TareaId))
            .ToListAsync();

        var notasList = tareas.Select(t => new AsignaturaNotaAlumnoDto
        {
            TareaId = t.TareaId,
            Valor = notas.FirstOrDefault(n => n.TareaId == t.TareaId)?.Valor
        }).ToList();

        decimal? Media(int trim)
        {
            var vals = tareas.Where(t => t.Trimestre == trim)
                .Select(t => notasList.First(n => n.TareaId == t.TareaId).Valor)
                .Where(v => v.HasValue).Select(v => v!.Value).ToList();
            return vals.Count > 0 ? Math.Round(vals.Average(), 2) : null;
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
            .Select(n => new { n.TareaId, n.EstudianteId, Alumno = n.Estudiante!.Nombre, n.Valor })
            .ToListAsync(cancellationToken);

        return tareas.Select(t => new TareaConNotasDto
        {
            TareaId = t.Id,
            Nombre = t.Nombre,
            Trimestre = t.Trimestre,
            AsignaturaId = t.AsignaturaId,
            Asignatura = asignaturaNombre,
            Notas = notas
                .Where(n => n.TareaId == t.Id)
                .OrderBy(n => n.Alumno)
                .Select(n => new TareaNotaAlumnoDto { EstudianteId = n.EstudianteId, Alumno = n.Alumno, Valor = n.Valor })
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
            .Where(i => i.ProfesorId == profesorId)
            .OrderBy(i => i.Curso!.Nombre)
            .ThenBy(i => i.Asignatura!.Nombre)
            .Select(i => new { i.AsignaturaId, Asignatura = i.Asignatura!.Nombre, Curso = i.Curso!.Nombre })
            .ToListAsync(cancellationToken);

        var asignaturaIds = asignaciones.Select(x => x.AsignaturaId).Distinct().ToList();
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
        var alumnoIds = alumnos.Select(a => a.EstudianteId).Distinct().ToList();
        var notaMap = await context.Notas
            .AsNoTracking()
            .Where(n => tareaIds.Contains(n.TareaId) && alumnoIds.Contains(n.EstudianteId))
            .ToDictionaryAsync(n => (n.EstudianteId, n.TareaId), n => (decimal?)n.Valor, cancellationToken);

        var mediasGlobales = new List<double>();
        var asignaturasStats = new List<AsignaturaStatsProfesorDto>();

        foreach (var asignacion in asignaciones)
        {
            var alumnosAsignatura = alumnos
                .Where(a => a.AsignaturaId == asignacion.AsignaturaId)
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
                .Where(a => a.NotaFinal.HasValue)
                .Select(a => (double)a.NotaFinal!.Value)
                .ToList();

            mediasGlobales.AddRange(finales);

            asignaturasStats.Add(new AsignaturaStatsProfesorDto
            {
                AsignaturaId = asignacion.AsignaturaId,
                Asignatura = asignacion.Asignatura,
                Curso = asignacion.Curso,
                TotalAlumnos = alumnosResumen.Count,
                Aprobados = alumnosResumen.Count(a => a.NotaFinal.HasValue && a.NotaFinal.Value >= 5),
                Suspensos = alumnosResumen.Count(a => a.NotaFinal.HasValue && a.NotaFinal.Value < 5),
                SinNota = alumnosResumen.Count(a => !a.NotaFinal.HasValue),
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
                .Where(t => t.Trimestre == trimestre)
                .Select(t => notaMap.TryGetValue((estudianteId, t.TareaId), out var valor) ? valor : null)
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
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
            .Where(v => v.HasValue)
            .Select(v => (double)v!.Value)
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

    public async Task<ProfesorListItemDto> CreateAsync(string nombre, string correo, string hash, CancellationToken cancellationToken = default)
    {
        var profesor = await context.Profesores
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Correo == correo, cancellationToken);

        if (profesor is null)
        {
            profesor = new Profesor { Nombre = nombre, Correo = correo, Contrasena = hash };
            context.Profesores.Add(profesor);
        }
        else
        {
            profesor.Nombre = nombre;
            profesor.Correo = correo;
            profesor.Contrasena = hash;
            profesor.IsDeleted = false;
        }

        await context.SaveChangesAsync(cancellationToken);
        return new ProfesorListItemDto { Id = profesor.Id, Nombre = profesor.Nombre, Correo = profesor.Correo, Imparticiones = new() };
    }

    public async Task<ProfesorListItemDto?> UpdateAsync(int id, string nombre, string correo, string? hash, CancellationToken cancellationToken = default)
    {
        var profesor = await context.Profesores.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (profesor is null) return null;

        profesor.Nombre = nombre;
        profesor.Correo = correo;
        if (hash is not null) profesor.Contrasena = hash;
        await context.SaveChangesAsync();

        var imparticiones = await context.ProfesorAsignaturaCursos
            .AsNoTracking()
            .Where(i => i.ProfesorId == id)
            .Select(i => new ProfesorImparticionDto
            {
                AsignaturaId = i.AsignaturaId,
                Asignatura = i.Asignatura!.Nombre,
                CursoId = i.CursoId,
                Curso = i.Curso!.Nombre
            })
            .ToListAsync();

        return new ProfesorListItemDto { Id = profesor.Id, Nombre = profesor.Nombre, Correo = profesor.Correo, Imparticiones = imparticiones };
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var imparticiones = await context.ProfesorAsignaturaCursos
            .Where(i => i.ProfesorId == id).ToListAsync(cancellationToken);
        context.ProfesorAsignaturaCursos.RemoveRange(imparticiones);

        var tareaIds = await context.Tareas
            .Where(t => t.ProfesorId == id).Select(t => t.Id).ToListAsync(cancellationToken);
        if (tareaIds.Count > 0)
        {
            var notas = await context.Notas.Where(n => tareaIds.Contains(n.TareaId)).ToListAsync(cancellationToken);
            context.Notas.RemoveRange(notas);
            var tareas = await context.Tareas.Where(t => t.ProfesorId == id).ToListAsync(cancellationToken);
            context.Tareas.RemoveRange(tareas);
        }

        var tokens = await context.RefreshTokens
            .Where(t => t.UserId == id && t.Rol == "profesor")
            .ToListAsync(cancellationToken);
        context.RefreshTokens.RemoveRange(tokens);

        var profesor = await context.Profesores.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (profesor is not null) context.Profesores.Remove(profesor);
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
            context.ProfesorAsignaturaCursos.Remove(registro);
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
        await context.SaveChangesAsync();

        return new TareaDetalleDto { Id = tarea.Id, Nombre = tarea.Nombre, Trimestre = tarea.Trimestre, AsignaturaId = tarea.AsignaturaId, Asignatura = asignaturaNombre };
    }

    #endregion
}

