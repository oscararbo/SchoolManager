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

    private sealed class EnrollmentFinalRow
    {
        public int EstudianteId { get; init; }
        public int CursoId { get; init; }
        public string Curso { get; init; } = string.Empty;
        public int AsignaturaId { get; init; }
        public string Asignatura { get; init; } = string.Empty;
        public double? NotaFinal { get; init; }
    }

    private sealed class CourseStudentPerformanceRow
    {
        public int CursoId { get; init; }
        public string Curso { get; init; } = string.Empty;
        public double? MediaAlumno { get; init; }
        public bool TieneNotasFinales { get; init; }
    }

    private sealed class CourseKpiAggregateRow
    {
        public int CursoId { get; init; }
        public string Curso { get; init; } = string.Empty;
        public int TotalAlumnos { get; init; }
        public int Aprobados { get; init; }
        public int Suspensos { get; init; }
        public double? Media { get; init; }
    }

    private sealed class SubjectKpiAggregateRow
    {
        public int AsignaturaId { get; init; }
        public string Asignatura { get; init; } = string.Empty;
        public int CursoId { get; init; }
        public string Curso { get; init; } = string.Empty;
        public int TotalAlumnos { get; init; }
        public int Aprobados { get; init; }
        public int Suspensos { get; init; }
        public int SinNota { get; init; }
        public double? Media { get; init; }
    }

    public async Task<AdminStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var totals = await GetTotalsAsync(cancellationToken);

        var enrollmentFinalRowsQuery = BuildEnrollmentFinalRowsQuery(context);

        var courseStudentPerformanceRowsQuery = BuildCourseStudentPerformanceRowsQuery(enrollmentFinalRowsQuery);
        var courseKpiAggregatesQuery = BuildCourseKpiAggregatesQuery(courseStudentPerformanceRowsQuery);
        var subjectKpiAggregatesQuery = BuildSubjectKpiAggregatesQuery(enrollmentFinalRowsQuery);

        var cursoConMejorMediaRaw = await courseKpiAggregatesQuery
            .Where(cursoRendimiento => cursoRendimiento.Media.HasValue)
            .OrderByDescending(cursoRendimiento => cursoRendimiento.Media)
            .FirstOrDefaultAsync(cancellationToken);

        var cursoConPeorMediaRaw = await courseKpiAggregatesQuery
            .Where(cursoRendimiento => cursoRendimiento.Media.HasValue)
            .OrderBy(cursoRendimiento => cursoRendimiento.Media)
            .FirstOrDefaultAsync(cancellationToken);

        var cursoConMejorMedia = cursoConMejorMediaRaw is null ? null : ToCursoResumenKpiDto(cursoConMejorMediaRaw);
        var cursoConPeorMedia = cursoConPeorMediaRaw is null ? null : ToCursoResumenKpiDto(cursoConPeorMediaRaw);

        var asignaturaConMejorMediaRaw = await subjectKpiAggregatesQuery
            .Where(asignaturaRendimiento => asignaturaRendimiento.Media.HasValue)
            .OrderByDescending(asignaturaRendimiento => asignaturaRendimiento.Media)
            .FirstOrDefaultAsync(cancellationToken);

        var asignaturaConPeorMediaRaw = await subjectKpiAggregatesQuery
            .Where(asignaturaRendimiento => asignaturaRendimiento.Media.HasValue)
            .OrderBy(asignaturaRendimiento => asignaturaRendimiento.Media)
            .FirstOrDefaultAsync(cancellationToken);

        var mediaGlobal = await subjectKpiAggregatesQuery
            .Where(asignaturaRendimiento => asignaturaRendimiento.Media.HasValue)
            .Select(asignaturaRendimiento => asignaturaRendimiento.Media)
            .AverageAsync(cancellationToken);

        var asignaturaConMejorMedia = asignaturaConMejorMediaRaw is null
            ? null
            : new AsignaturaResumenKpiDto
            {
                Asignatura = asignaturaConMejorMediaRaw.Asignatura,
                Curso = asignaturaConMejorMediaRaw.Curso,
                Media = asignaturaConMejorMediaRaw.Media.HasValue
                    ? Math.Round(asignaturaConMejorMediaRaw.Media.Value, 2)
                    : null
            };

        var asignaturaConPeorMedia = asignaturaConPeorMediaRaw is null
            ? null
            : new AsignaturaResumenKpiDto
            {
                Asignatura = asignaturaConPeorMediaRaw.Asignatura,
                Curso = asignaturaConPeorMediaRaw.Curso,
                Media = asignaturaConPeorMediaRaw.Media.HasValue
                    ? Math.Round(asignaturaConPeorMediaRaw.Media.Value, 2)
                    : null
            };

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
            AsignaturaConPeorMedia = asignaturaConPeorMedia
        };
    }

    public async Task<AdminTop5StatsDto> GetTop5Async(CancellationToken cancellationToken = default)
    {
        var enrollmentFinalRowsQuery = BuildEnrollmentFinalRowsQuery(context);
        var courseStudentPerformanceRowsQuery = BuildCourseStudentPerformanceRowsQuery(enrollmentFinalRowsQuery);

        var courseKpiAggregates = await BuildCourseKpiAggregatesQuery(courseStudentPerformanceRowsQuery)
            .Where(curso => curso.Media.HasValue)
            .ToListAsync(cancellationToken);

        var subjectKpiAggregates = await BuildSubjectKpiAggregatesQuery(enrollmentFinalRowsQuery)
            .Where(asignatura => asignatura.Media.HasValue)
            .ToListAsync(cancellationToken);

        return new AdminTop5StatsDto
        {
            TopCursos = courseKpiAggregates
                .OrderByDescending(curso => curso.Media)
                .Take(5)
                .Select(ToCursoResumenKpiDto)
                .ToList(),
            BottomCursos = courseKpiAggregates
                .OrderBy(curso => curso.Media)
                .Take(5)
                .Select(ToCursoResumenKpiDto)
                .ToList(),
            TopAsignaturas = subjectKpiAggregates
                .OrderByDescending(asignatura => asignatura.Media)
                .Take(5)
                .Select(ToAsignaturaTop5ItemDto)
                .ToList(),
            BottomAsignaturas = subjectKpiAggregates
                .OrderBy(asignatura => asignatura.Media)
                .Take(5)
                .Select(ToAsignaturaTop5ItemDto)
                .ToList()
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

        var courseStudentPerformanceRowsQuery = BuildCourseStudentPerformanceRowsQuery(enrollmentFinalRowsQuery);
        var resumenCurso = await BuildCourseKpiAggregatesQuery(courseStudentPerformanceRowsQuery)
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

        var asignaturaDestacada = asignaturas
            .OrderByDescending(asignaturaStats => asignaturaStats.PorcentajeAprobados)
            .ThenByDescending(asignaturaStats => asignaturaStats.Media)
            .FirstOrDefault();

        var asignaturaAVigilar = asignaturas
            .OrderByDescending(asignaturaStats => asignaturaStats.PorcentajeSuspensos)
            .ThenBy(asignaturaStats => asignaturaStats.Media)
            .FirstOrDefault();

        var totalAlumnos = resumenCurso?.TotalAlumnos ?? 0;
        var aprobados = resumenCurso?.Aprobados ?? 0;
        var suspensos = resumenCurso?.Suspensos ?? 0;
        var sinNota = totalAlumnos - aprobados - suspensos;

        return new CursoNotasStatsResponseDto
        {
            CursoId = curso.Id,
            Curso = curso.Nombre,
            MediaGlobalCurso = resumenCurso?.Media is double mediaCurso ? Math.Round(mediaCurso, 2) : null,
            TotalAlumnos = totalAlumnos,
            Aprobados = aprobados,
            Suspensos = suspensos,
            SinNota = sinNota,
            PorcentajeAprobados = ToPercentage(aprobados, totalAlumnos),
            PorcentajeSuspensos = ToPercentage(suspensos, totalAlumnos),
            AsignaturaDestacada = asignaturaDestacada,
            AsignaturaAVigilar = asignaturaAVigilar,
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

        var courseStudentPerformanceRowsQuery = BuildCourseStudentPerformanceRowsQuery(enrollmentFinalRowsQuery);
        var comparisonRaw = await BuildCourseKpiAggregatesQuery(courseStudentPerformanceRowsQuery)
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
                SinNota = cursoStats.TotalAlumnos - cursoStats.Aprobados - cursoStats.Suspensos,
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
                EstudianteId = estudianteAsignatura.EstudianteId,
                CursoId = estudianteAsignatura.Asignatura!.CursoId,
                Curso = estudianteAsignatura.Asignatura!.Curso!.Nombre,
                AsignaturaId = estudianteAsignatura.AsignaturaId,
                Asignatura = estudianteAsignatura.Asignatura!.Nombre,
                NotaFinal = finalGrade != null ? finalGrade.NotaFinal : null
            };
    }

    private static IQueryable<CourseStudentPerformanceRow> BuildCourseStudentPerformanceRowsQuery(IQueryable<EnrollmentFinalRow> enrollmentFinalRowsQuery)
    {
        return
            from enrollmentFinalRow in enrollmentFinalRowsQuery
            group enrollmentFinalRow by new { enrollmentFinalRow.CursoId, enrollmentFinalRow.Curso, enrollmentFinalRow.EstudianteId } into groupedByCourseStudent
            select new CourseStudentPerformanceRow
            {
                CursoId = groupedByCourseStudent.Key.CursoId,
                Curso = groupedByCourseStudent.Key.Curso,
                MediaAlumno = groupedByCourseStudent.Where(enrollmentFinalRowItem => enrollmentFinalRowItem.NotaFinal.HasValue)
                    .Average(enrollmentFinalRowItem => enrollmentFinalRowItem.NotaFinal),
                TieneNotasFinales = groupedByCourseStudent.Any(enrollmentFinalRowItem => enrollmentFinalRowItem.NotaFinal.HasValue)
            };
    }

    private static IQueryable<CourseKpiAggregateRow> BuildCourseKpiAggregatesQuery(IQueryable<CourseStudentPerformanceRow> courseStudentPerformanceRowsQuery)
    {
        return
            from courseStudentPerformanceRow in courseStudentPerformanceRowsQuery
            group courseStudentPerformanceRow by new { courseStudentPerformanceRow.CursoId, courseStudentPerformanceRow.Curso } into groupedByCurso
            select new CourseKpiAggregateRow
            {
                CursoId = groupedByCurso.Key.CursoId,
                Curso = groupedByCurso.Key.Curso,
                TotalAlumnos = groupedByCurso.Count(),
                Aprobados = groupedByCurso.Sum(courseStudentPerformanceRowItem =>
                    courseStudentPerformanceRowItem.TieneNotasFinales
                    && courseStudentPerformanceRowItem.MediaAlumno.HasValue
                    && courseStudentPerformanceRowItem.MediaAlumno.Value >= 5d
                        ? 1
                        : 0),
                Suspensos = groupedByCurso.Sum(courseStudentPerformanceRowItem =>
                    courseStudentPerformanceRowItem.TieneNotasFinales
                    && courseStudentPerformanceRowItem.MediaAlumno.HasValue
                    && courseStudentPerformanceRowItem.MediaAlumno.Value < 5d
                        ? 1
                        : 0),
                Media = groupedByCurso.Where(courseStudentPerformanceRowItem =>
                        courseStudentPerformanceRowItem.TieneNotasFinales
                        && courseStudentPerformanceRowItem.MediaAlumno.HasValue)
                    .Average(courseStudentPerformanceRowItem => courseStudentPerformanceRowItem.MediaAlumno)
            };
    }

    private static IQueryable<SubjectKpiAggregateRow> BuildSubjectKpiAggregatesQuery(IQueryable<EnrollmentFinalRow> enrollmentFinalRowsQuery)
    {
        return
            from enrollmentFinalRow in enrollmentFinalRowsQuery
            group enrollmentFinalRow by new
            {
                enrollmentFinalRow.AsignaturaId,
                enrollmentFinalRow.Asignatura,
                enrollmentFinalRow.CursoId,
                enrollmentFinalRow.Curso
            }
            into groupedByAsignatura
            select new SubjectKpiAggregateRow
            {
                AsignaturaId = groupedByAsignatura.Key.AsignaturaId,
                Asignatura = groupedByAsignatura.Key.Asignatura,
                CursoId = groupedByAsignatura.Key.CursoId,
                Curso = groupedByAsignatura.Key.Curso,
                TotalAlumnos = groupedByAsignatura.Count(),
                Aprobados = groupedByAsignatura.Sum(enrollmentFinalRowItem =>
                    enrollmentFinalRowItem.NotaFinal.HasValue && enrollmentFinalRowItem.NotaFinal.Value >= 5d ? 1 : 0),
                Suspensos = groupedByAsignatura.Sum(enrollmentFinalRowItem =>
                    enrollmentFinalRowItem.NotaFinal.HasValue && enrollmentFinalRowItem.NotaFinal.Value < 5d ? 1 : 0),
                SinNota = groupedByAsignatura.Sum(enrollmentFinalRowItem =>
                    enrollmentFinalRowItem.NotaFinal.HasValue ? 0 : 1),
                Media = groupedByAsignatura.Where(enrollmentFinalRowItem => enrollmentFinalRowItem.NotaFinal.HasValue)
                    .Average(enrollmentFinalRowItem => enrollmentFinalRowItem.NotaFinal)
            };
    }

    private static CursoResumenKpiDto ToCursoResumenKpiDto(CourseKpiAggregateRow cursoRendimiento)
    {
        return new CursoResumenKpiDto
        {
            Curso = cursoRendimiento.Curso,
            MediaGlobalCurso = cursoRendimiento.Media.HasValue ? Math.Round(cursoRendimiento.Media.Value, 2) : null,
            PorcentajeAprobados = ToPercentage(cursoRendimiento.Aprobados, cursoRendimiento.TotalAlumnos),
            PorcentajeSuspensos = ToPercentage(cursoRendimiento.Suspensos, cursoRendimiento.TotalAlumnos)
        };
    }

    private static AsignaturaTop5ItemDto ToAsignaturaTop5ItemDto(SubjectKpiAggregateRow asignaturaRendimiento)
    {
        return new AsignaturaTop5ItemDto
        {
            AsignaturaId = asignaturaRendimiento.AsignaturaId,
            Asignatura = asignaturaRendimiento.Asignatura,
            Curso = asignaturaRendimiento.Curso,
            Media = asignaturaRendimiento.Media.HasValue ? Math.Round(asignaturaRendimiento.Media.Value, 2) : null,
            PorcentajeAprobados = ToPercentage(asignaturaRendimiento.Aprobados, asignaturaRendimiento.TotalAlumnos),
            PorcentajeSuspensos = ToPercentage(asignaturaRendimiento.Suspensos, asignaturaRendimiento.TotalAlumnos)
        };
    }

    private static double ToPercentage(int part, int total)
    {
        if (total <= 0)
            return 0;

        return Math.Round((double)part * 100d / total, 2);
    }
}
