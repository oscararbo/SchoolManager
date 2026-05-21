using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Dtos;
using Back.Api.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Back.Api.Persistence.Repositories;

public class AdminStatsDomainRepository(AppDbContext context) : IAdminStatsDomainRepository
{
    private sealed class FinalGradeRow
    {
        public int EstudianteId { get; init; }
        public int AsignaturaId { get; init; }
        public double? NotaFinal { get; init; }
    }

    private sealed class AggregationRow
    {
        public int TotalAlumnos { get; init; }
        public int Aprobados { get; init; }
        public int Suspensos { get; init; }
        public int SinNota { get; init; }
        public double? Media { get; init; }
    }

    private sealed class EnrollmentFinalRow
    {
        public int CursoId { get; init; }
        public string Curso { get; init; } = string.Empty;
        public int AsignaturaId { get; init; }
        public string Asignatura { get; init; } = string.Empty;
        public double? NotaFinal { get; init; }
    }

    public async Task<AdminStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var totals = await GetTotalsAsync(cancellationToken);

        var porCurso = await context.Cursos
            .AsNoTracking()
            .OrderBy(curso => curso.Nombre)
            .Select(curso => new CursoStatsItemDto
            {
                Curso = curso.Nombre,
                Estudiantes = context.Estudiantes.Count(estudiante => estudiante.CursoId == curso.Id),
                Asignaturas = context.Asignaturas.Count(asignatura => asignatura.CursoId == curso.Id)
            })
            .ToListAsync(cancellationToken);

        var enrollmentFinalRowsQuery = BuildEnrollmentFinalRowsQuery(context);

        var rendimientoCursos = await (
            from enrollmentFinalRow in enrollmentFinalRowsQuery
            group enrollmentFinalRow by new { enrollmentFinalRow.CursoId, enrollmentFinalRow.Curso } into groupedByCurso
            select new
            {
                groupedByCurso.Key.CursoId,
                groupedByCurso.Key.Curso,
                TotalAlumnos = groupedByCurso.Count(),
                Aprobados = groupedByCurso.Sum(enrollmentFinalRowItem => enrollmentFinalRowItem.NotaFinal.HasValue && enrollmentFinalRowItem.NotaFinal.Value >= 5d ? 1 : 0),
                Suspensos = groupedByCurso.Sum(enrollmentFinalRowItem => enrollmentFinalRowItem.NotaFinal.HasValue && enrollmentFinalRowItem.NotaFinal.Value < 5d ? 1 : 0),
                SinNota = groupedByCurso.Sum(enrollmentFinalRowItem => enrollmentFinalRowItem.NotaFinal.HasValue ? 0 : 1),
                Media = groupedByCurso.Where(enrollmentFinalRowItem => enrollmentFinalRowItem.NotaFinal.HasValue)
                    .Average(enrollmentFinalRowItem => enrollmentFinalRowItem.NotaFinal)
            })
            .OrderBy(cursoStats => cursoStats.Curso)
            .ToListAsync(cancellationToken);

        var rendimientoAsignaturas = await (
            from enrollmentFinalRow in enrollmentFinalRowsQuery
            group enrollmentFinalRow by new { enrollmentFinalRow.AsignaturaId, enrollmentFinalRow.Asignatura, enrollmentFinalRow.CursoId, enrollmentFinalRow.Curso } into groupedByAsignatura
            select new
            {
                groupedByAsignatura.Key.AsignaturaId,
                groupedByAsignatura.Key.Asignatura,
                groupedByAsignatura.Key.CursoId,
                groupedByAsignatura.Key.Curso,
                TotalAlumnos = groupedByAsignatura.Count(),
                Aprobados = groupedByAsignatura.Sum(enrollmentFinalRowItem => enrollmentFinalRowItem.NotaFinal.HasValue && enrollmentFinalRowItem.NotaFinal.Value >= 5d ? 1 : 0),
                Suspensos = groupedByAsignatura.Sum(enrollmentFinalRowItem => enrollmentFinalRowItem.NotaFinal.HasValue && enrollmentFinalRowItem.NotaFinal.Value < 5d ? 1 : 0),
                SinNota = groupedByAsignatura.Sum(enrollmentFinalRowItem => enrollmentFinalRowItem.NotaFinal.HasValue ? 0 : 1),
                Media = groupedByAsignatura.Where(enrollmentFinalRowItem => enrollmentFinalRowItem.NotaFinal.HasValue)
                    .Average(enrollmentFinalRowItem => enrollmentFinalRowItem.NotaFinal)
            })
            .OrderBy(asignaturaStats => asignaturaStats.Curso)
            .ThenBy(asignaturaStats => asignaturaStats.Asignatura)
            .ToListAsync(cancellationToken);

        var rendimientoCursoDtos = rendimientoCursos.Select(cursoStats => new CursoRendimientoDto
        {
            CursoId = cursoStats.CursoId,
            Curso = cursoStats.Curso,
            TotalAlumnos = cursoStats.TotalAlumnos,
            Aprobados = cursoStats.Aprobados,
            Suspensos = cursoStats.Suspensos,
            SinNota = cursoStats.SinNota,
            MediaGlobalCurso = cursoStats.Media.HasValue ? Math.Round(cursoStats.Media.Value, 2) : null,
            PorcentajeAprobados = ToPercentage(cursoStats.Aprobados, cursoStats.TotalAlumnos),
            PorcentajeSuspensos = ToPercentage(cursoStats.Suspensos, cursoStats.TotalAlumnos)
        }).ToList();

        var rendimientoAsignaturaDtos = rendimientoAsignaturas.Select(asignaturaStats => new AsignaturaRendimientoDto
        {
            AsignaturaId = asignaturaStats.AsignaturaId,
            Asignatura = asignaturaStats.Asignatura,
            CursoId = asignaturaStats.CursoId,
            Curso = asignaturaStats.Curso,
            TotalAlumnos = asignaturaStats.TotalAlumnos,
            Aprobados = asignaturaStats.Aprobados,
            Suspensos = asignaturaStats.Suspensos,
            SinNota = asignaturaStats.SinNota,
            Media = asignaturaStats.Media.HasValue ? Math.Round(asignaturaStats.Media.Value, 2) : null,
            PorcentajeAprobados = ToPercentage(asignaturaStats.Aprobados, asignaturaStats.TotalAlumnos),
            PorcentajeSuspensos = ToPercentage(asignaturaStats.Suspensos, asignaturaStats.TotalAlumnos)
        }).ToList();

        var cursoConMejorMedia = rendimientoCursoDtos
            .Where(cursoRendimiento => cursoRendimiento.MediaGlobalCurso.HasValue)
            .OrderByDescending(cursoRendimiento => cursoRendimiento.MediaGlobalCurso)
            .FirstOrDefault();

        var cursoConPeorMedia = rendimientoCursoDtos
            .Where(cursoRendimiento => cursoRendimiento.MediaGlobalCurso.HasValue)
            .OrderBy(cursoRendimiento => cursoRendimiento.MediaGlobalCurso)
            .FirstOrDefault();

        var asignaturaConMejorMedia = rendimientoAsignaturaDtos
            .Where(asignaturaRendimiento => asignaturaRendimiento.Media.HasValue)
            .OrderByDescending(asignaturaRendimiento => asignaturaRendimiento.Media)
            .FirstOrDefault();

        var asignaturaConPeorMedia = rendimientoAsignaturaDtos
            .Where(asignaturaRendimiento => asignaturaRendimiento.Media.HasValue)
            .OrderBy(asignaturaRendimiento => asignaturaRendimiento.Media)
            .FirstOrDefault();

        var mediasGlobales = rendimientoAsignaturaDtos
            .Where(asignaturaRendimiento => asignaturaRendimiento.Media.HasValue)
            .Select(asignaturaRendimiento => asignaturaRendimiento.Media!.Value)
            .ToList();

        var mediaGlobal = mediasGlobales.Count > 0 ? mediasGlobales.Average() : (double?)null;

        return new AdminStatsDto
        {
            TotalCursos = totals.TotalCursos,
            TotalAsignaturas = totals.TotalAsignaturas,
            TotalProfesores = totals.TotalProfesores,
            TotalEstudiantes = totals.TotalEstudiantes,
            TotalMatriculas = totals.TotalMatriculas,
            TotalTareas = totals.TotalTareas,
            MediaGlobal = mediaGlobal.HasValue ? Math.Round(mediaGlobal.Value, 2) : null,
            CursoConMejorMedia = cursoConMejorMedia,
            CursoConPeorMedia = cursoConPeorMedia,
            AsignaturaConMejorMedia = asignaturaConMejorMedia,
            AsignaturaConPeorMedia = asignaturaConPeorMedia,
            PorCurso = porCurso,
            RendimientoPorCurso = rendimientoCursoDtos,
            RendimientoPorAsignatura = rendimientoAsignaturaDtos
        };
    }

    public async Task<IEnumerable<CursoStatsSelectorDto>> GetCursosStatsSelectorAsync(CancellationToken cancellationToken = default)
        => await context.Cursos
            .AsNoTracking()
            .OrderBy(curso => curso.Nombre)
            .Select(curso => new CursoStatsSelectorDto
            {
                CursoId = curso.Id,
                Curso = curso.Nombre,
                TotalEstudiantes = context.Estudiantes.Count(estudiante => estudiante.CursoId == curso.Id),
                TotalAsignaturas = context.Asignaturas.Count(asignatura => asignatura.CursoId == curso.Id)
            })
            .ToListAsync(cancellationToken);

    public async Task<CursoNotasStatsResponseDto?> GetStatsByCursoAsync(int cursoId, CancellationToken cancellationToken = default)
    {
        var curso = await context.Cursos
            .AsNoTracking()
            .Where(curso => curso.Id == cursoId)
            .Select(curso => new { curso.Id, curso.Nombre })
            .FirstOrDefaultAsync(cancellationToken);

        if (curso is null)
            return null;

        var enrollmentFinalRowsQuery = BuildEnrollmentFinalRowsQuery(context).Where(enrollmentFinalRow => enrollmentFinalRow.CursoId == cursoId);

        var asignaturasRaw = await (
            from enrollmentFinalRow in enrollmentFinalRowsQuery
            group enrollmentFinalRow by new { enrollmentFinalRow.AsignaturaId, enrollmentFinalRow.Asignatura } into groupedByAsignatura
            select new
            {
                groupedByAsignatura.Key.AsignaturaId,
                groupedByAsignatura.Key.Asignatura,
                TotalAlumnos = groupedByAsignatura.Count(),
                Aprobados = groupedByAsignatura.Sum(enrollmentFinalRowItem => enrollmentFinalRowItem.NotaFinal.HasValue && enrollmentFinalRowItem.NotaFinal.Value >= 5d ? 1 : 0),
                Suspensos = groupedByAsignatura.Sum(enrollmentFinalRowItem => enrollmentFinalRowItem.NotaFinal.HasValue && enrollmentFinalRowItem.NotaFinal.Value < 5d ? 1 : 0),
                SinNota = groupedByAsignatura.Sum(enrollmentFinalRowItem => enrollmentFinalRowItem.NotaFinal.HasValue ? 0 : 1),
                Media = groupedByAsignatura.Where(enrollmentFinalRowItem => enrollmentFinalRowItem.NotaFinal.HasValue)
                    .Average(enrollmentFinalRowItem => enrollmentFinalRowItem.NotaFinal)
            })
            .OrderBy(asignaturaStats => asignaturaStats.Asignatura)
            .ToListAsync(cancellationToken);

        var resumen = await (
            from enrollmentFinalRow in enrollmentFinalRowsQuery
            group enrollmentFinalRow by 1 into groupedResumen
            select new AggregationRow
            {
                TotalAlumnos = groupedResumen.Count(),
                Aprobados = groupedResumen.Sum(enrollmentFinalRowItem => enrollmentFinalRowItem.NotaFinal.HasValue && enrollmentFinalRowItem.NotaFinal.Value >= 5d ? 1 : 0),
                Suspensos = groupedResumen.Sum(enrollmentFinalRowItem => enrollmentFinalRowItem.NotaFinal.HasValue && enrollmentFinalRowItem.NotaFinal.Value < 5d ? 1 : 0),
                SinNota = groupedResumen.Sum(enrollmentFinalRowItem => enrollmentFinalRowItem.NotaFinal.HasValue ? 0 : 1),
                Media = groupedResumen.Where(enrollmentFinalRowItem => enrollmentFinalRowItem.NotaFinal.HasValue)
                    .Average(enrollmentFinalRowItem => enrollmentFinalRowItem.NotaFinal)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var asignaturas = asignaturasRaw.Select(asignaturaStats => new AsignaturaNotasStatsDto
        {
            AsignaturaId = asignaturaStats.AsignaturaId,
            Asignatura = asignaturaStats.Asignatura,
            TotalAlumnos = asignaturaStats.TotalAlumnos,
            Aprobados = asignaturaStats.Aprobados,
            Suspensos = asignaturaStats.Suspensos,
            SinNota = asignaturaStats.SinNota,
            Media = asignaturaStats.Media.HasValue ? Math.Round(asignaturaStats.Media.Value, 2) : null,
            PorcentajeAprobados = ToPercentage(asignaturaStats.Aprobados, asignaturaStats.TotalAlumnos),
            PorcentajeSuspensos = ToPercentage(asignaturaStats.Suspensos, asignaturaStats.TotalAlumnos)
        }).ToList();

        var asignaturaConMejorMedia = asignaturas
            .Where(asignaturaStats => asignaturaStats.Media.HasValue)
            .OrderByDescending(asignaturaStats => asignaturaStats.Media)
            .FirstOrDefault();

        var asignaturaConPeorMedia = asignaturas
            .Where(asignaturaStats => asignaturaStats.Media.HasValue)
            .OrderBy(asignaturaStats => asignaturaStats.Media)
            .FirstOrDefault();

        var totalAlumnos = resumen?.TotalAlumnos ?? 0;
        var aprobados = resumen?.Aprobados ?? 0;
        var suspensos = resumen?.Suspensos ?? 0;
        var sinNota = resumen?.SinNota ?? 0;

        return new CursoNotasStatsResponseDto
        {
            CursoId = curso.Id,
            Curso = curso.Nombre,
            MediaGlobalCurso = resumen?.Media is double mediaCurso ? Math.Round(mediaCurso, 2) : null,
            TotalAlumnos = totalAlumnos,
            Aprobados = aprobados,
            Suspensos = suspensos,
            SinNota = sinNota,
            PorcentajeAprobados = ToPercentage(aprobados, totalAlumnos),
            PorcentajeSuspensos = ToPercentage(suspensos, totalAlumnos),
            AsignaturaConMejorMedia = asignaturaConMejorMedia,
            AsignaturaConPeorMedia = asignaturaConPeorMedia,
            Asignaturas = asignaturas
        };
    }

    public async Task<IEnumerable<CursoComparacionItemDto>> CompareCursosAsync(IEnumerable<int> cursoIds, CancellationToken cancellationToken = default)
    {
        var ids = cursoIds.Where(id => id > 0).Distinct().ToList();
        if (ids.Count == 0)
            return [];

        var cursos = await context.Cursos
            .AsNoTracking()
            .Where(curso => ids.Contains(curso.Id))
            .Select(curso => new { curso.Id, curso.Nombre })
            .ToListAsync(cancellationToken);

        var enrollmentFinalRowsQuery = BuildEnrollmentFinalRowsQuery(context)
            .Where(enrollmentFinalRow => ids.Contains(enrollmentFinalRow.CursoId));

        var comparisonRaw = await (
            from enrollmentFinalRow in enrollmentFinalRowsQuery
            group enrollmentFinalRow by new { enrollmentFinalRow.CursoId, enrollmentFinalRow.Curso } into groupedByCurso
            select new
            {
                groupedByCurso.Key.CursoId,
                groupedByCurso.Key.Curso,
                TotalAlumnos = groupedByCurso.Count(),
                Aprobados = groupedByCurso.Sum(enrollmentFinalRowItem => enrollmentFinalRowItem.NotaFinal.HasValue && enrollmentFinalRowItem.NotaFinal.Value >= 5d ? 1 : 0),
                Suspensos = groupedByCurso.Sum(enrollmentFinalRowItem => enrollmentFinalRowItem.NotaFinal.HasValue && enrollmentFinalRowItem.NotaFinal.Value < 5d ? 1 : 0),
                SinNota = groupedByCurso.Sum(enrollmentFinalRowItem => enrollmentFinalRowItem.NotaFinal.HasValue ? 0 : 1),
                Media = groupedByCurso.Where(enrollmentFinalRowItem => enrollmentFinalRowItem.NotaFinal.HasValue)
                    .Average(enrollmentFinalRowItem => enrollmentFinalRowItem.NotaFinal)
            })
            .ToListAsync(cancellationToken);

        var map = comparisonRaw.ToDictionary(
            cursoStats => cursoStats.CursoId,
            cursoStats => new CursoComparacionItemDto
            {
                CursoId = cursoStats.CursoId,
                Curso = cursoStats.Curso,
                MediaGlobalCurso = cursoStats.Media.HasValue ? Math.Round(cursoStats.Media.Value, 2) : null,
                TotalAlumnos = cursoStats.TotalAlumnos,
                Aprobados = cursoStats.Aprobados,
                Suspensos = cursoStats.Suspensos,
                SinNota = cursoStats.SinNota,
                PorcentajeAprobados = ToPercentage(cursoStats.Aprobados, cursoStats.TotalAlumnos),
                PorcentajeSuspensos = ToPercentage(cursoStats.Suspensos, cursoStats.TotalAlumnos)
            });

        return cursos
            .Select(cursoItem => map.TryGetValue(cursoItem.Id, out var cursoStatsDto)
                ? cursoStatsDto
                : new CursoComparacionItemDto
                {
                    CursoId = cursoItem.Id,
                    Curso = cursoItem.Nombre,
                    MediaGlobalCurso = null,
                    TotalAlumnos = 0,
                    Aprobados = 0,
                    Suspensos = 0,
                    SinNota = 0,
                    PorcentajeAprobados = 0,
                    PorcentajeSuspensos = 0
                })
            .OrderBy(cursoStats => cursoStats.Curso)
            .ToList();
    }

    private async Task<(int TotalCursos, int TotalAsignaturas, int TotalProfesores, int TotalEstudiantes, int TotalMatriculas, int TotalTareas)> GetTotalsAsync(CancellationToken cancellationToken)
    {
        var totalCursos = await context.Cursos.CountAsync(cancellationToken);
        var totalAsignaturas = await context.Asignaturas.CountAsync(cancellationToken);
        var totalProfesores = await context.Profesores.CountAsync(cancellationToken);
        var totalEstudiantes = await context.Estudiantes.CountAsync(cancellationToken);
        var totalMatriculas = await context.EstudianteAsignaturas.CountAsync(cancellationToken);
        var totalTareas = await context.Tareas.CountAsync(cancellationToken);

        return (
            totalCursos,
            totalAsignaturas,
            totalProfesores,
            totalEstudiantes,
            totalMatriculas,
            totalTareas
        );
    }

    private static IQueryable<FinalGradeRow> BuildFinalGradesQuery(AppDbContext dbContext)
    {
        var trimesterAverages =
            from nota in dbContext.Notas.AsNoTracking()
            join tarea in dbContext.Tareas.AsNoTracking() on nota.TareaId equals tarea.Id
            group nota by new { nota.EstudianteId, tarea.AsignaturaId, tarea.Trimestre } into groupedByTrimestre
            select new
            {
                groupedByTrimestre.Key.EstudianteId,
                groupedByTrimestre.Key.AsignaturaId,
                MediaTrimestre = groupedByTrimestre.Average(notaTrimestral => (double)notaTrimestral.Valor)
            };

        return
            from promedioTrimestral in trimesterAverages
            group promedioTrimestral by new { promedioTrimestral.EstudianteId, promedioTrimestral.AsignaturaId } into groupedByAsignaturaEstudiante
            select new FinalGradeRow
            {
                EstudianteId = groupedByAsignaturaEstudiante.Key.EstudianteId,
                AsignaturaId = groupedByAsignaturaEstudiante.Key.AsignaturaId,
                NotaFinal = groupedByAsignaturaEstudiante.Count() == 3
                    ? groupedByAsignaturaEstudiante.Average(promedioTrimestral => promedioTrimestral.MediaTrimestre)
                    : (double?)null
            };
    }

    private static IQueryable<EnrollmentFinalRow> BuildEnrollmentFinalRowsQuery(AppDbContext dbContext)
    {
        var finalGradesQuery = BuildFinalGradesQuery(dbContext);

        return
            from estudianteAsignatura in dbContext.EstudianteAsignaturas.AsNoTracking()
            join finalGrade in finalGradesQuery
                on new { estudianteAsignatura.EstudianteId, estudianteAsignatura.AsignaturaId }
                equals new { finalGrade.EstudianteId, finalGrade.AsignaturaId }
                into finalGradesJoin
            from finalGrade in finalGradesJoin.DefaultIfEmpty()
            select new EnrollmentFinalRow
            {
                CursoId = estudianteAsignatura.Asignatura!.CursoId,
                Curso = estudianteAsignatura.Asignatura!.Curso!.Nombre,
                AsignaturaId = estudianteAsignatura.AsignaturaId,
                Asignatura = estudianteAsignatura.Asignatura!.Nombre,
                NotaFinal = finalGrade != null ? finalGrade.NotaFinal : null
            };
    }

    private static double ToPercentage(int part, int total)
    {
        if (total <= 0)
            return 0;

        return Math.Round((double)part * 100d / total, 2);
    }
}
