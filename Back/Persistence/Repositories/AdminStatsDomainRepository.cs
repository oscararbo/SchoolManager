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

        var enrollmentFinalRows = await BuildEnrollmentFinalRowsQuery(context)
            .ToListAsync(cancellationToken);

        var courseKpis = enrollmentFinalRows
            .GroupBy(x => new { x.CursoId, x.Curso })
            .Select(g =>
            {
                var conNota = g.Where(x => x.NotaFinal.HasValue).ToList();

                return new CourseKpiAggregateRow
                {
                    CursoId = g.Key.CursoId,
                    Curso = g.Key.Curso,
                    TotalAlumnos = g.Count(),
                    Aprobados = conNota.Count(x => x.NotaFinal >= 5),
                    Suspensos = conNota.Count(x => x.NotaFinal < 5),
                    Media = conNota.Count > 0
                        ? conNota.Average(x => x.NotaFinal) 
                        : null
                };
            })
            .ToList();

        var subjectKpis = enrollmentFinalRows
            .GroupBy(x => new { x.AsignaturaId, x.Asignatura, x.Curso })
            .Select(g =>
            {
                var conNota = g.Where(x => x.NotaFinal.HasValue).ToList();

                return new SubjectKpiAggregateRow
                {
                    AsignaturaId = g.Key.AsignaturaId,
                    Asignatura = g.Key.Asignatura,
                    Curso = g.Key.Curso,
                    CursoId = g.First().CursoId,
                    TotalAlumnos = g.Count(),
                    Aprobados = conNota.Count(x => x.NotaFinal >= 5),
                    Suspensos = conNota.Count(x => x.NotaFinal < 5),
                    SinNota = g.Count(x => !x.NotaFinal.HasValue),
                    Media = conNota.Count > 0
                        ? conNota.Average(x => x.NotaFinal)
                        : null
                };
            })
            .ToList();

        var mejorCurso = courseKpis
            .Where(x => x.Media.HasValue)
            .OrderByDescending(x => x.Media)
            .FirstOrDefault();

        var peorCurso = courseKpis
            .Where(x => x.Media.HasValue)
            .OrderBy(x => x.Media)
            .FirstOrDefault();

        var mejorAsignatura = subjectKpis
            .Where(x => x.Media.HasValue)
            .OrderByDescending(x => x.Media)
            .FirstOrDefault();

        var peorAsignatura = subjectKpis
            .Where(x => x.Media.HasValue)
            .OrderBy(x => x.Media)
            .FirstOrDefault();

        var medias = subjectKpis
            .Where(x => x.Media.HasValue)
            .Select(x => x.Media!.Value)
            .ToList();

        double? mediaGlobal = medias.Count > 0
            ? Math.Round(medias.Average(), 2)
            : null;

        return new AdminStatsDto
        {
            TotalCursos = totals.TotalCursos,
            TotalAsignaturas = totals.TotalAsignaturas,
            TotalProfesores = totals.TotalProfesores,
            TotalEstudiantes = totals.TotalEstudiantes,
            TotalMatriculas = totals.TotalMatriculas,
            TotalTareas = totals.TotalTareas,

            MediaGlobal = mediaGlobal,

            CursoConMejorMedia = mejorCurso != null
                ? ToCursoResumenKpiDto(mejorCurso)
                : null,

            CursoConPeorMedia = peorCurso != null
                ? ToCursoResumenKpiDto(peorCurso)
                : null,

            AsignaturaConMejorMedia = mejorAsignatura != null
                ? new AsignaturaResumenKpiDto
                {
                    Asignatura = mejorAsignatura.Asignatura,
                    Curso = mejorAsignatura.Curso,
                    Media = mejorAsignatura.Media.HasValue
                        ? Math.Round(mejorAsignatura.Media.Value, 2)
                        : null
                }
                : null,

            AsignaturaConPeorMedia = peorAsignatura != null
                ? new AsignaturaResumenKpiDto
                {
                    Asignatura = peorAsignatura.Asignatura,
                    Curso = peorAsignatura.Curso,
                    Media = peorAsignatura.Media.HasValue
                        ? Math.Round(peorAsignatura.Media.Value, 2)
                        : null
                }
                : null
        };
    }

    public async Task<AdminTop5StatsDto> GetTop5Async(CancellationToken cancellationToken = default)
    {
        var rows = await BuildEnrollmentFinalRowsQuery(context)
            .Where(x => x.NotaFinal.HasValue)
            .ToListAsync(cancellationToken);

        var courseKpis = rows
            .GroupBy(x => new { x.CursoId, x.Curso })
            .Select(g => new
            {
                g.Key.CursoId,
                g.Key.Curso,
                Media = g.Average(x => x.NotaFinal!.Value),
                TotalAlumnos = g.Count(),
                Aprobados = g.Count(x => x.NotaFinal >= 5),
                Suspensos = g.Count(x => x.NotaFinal < 5)
            })
            .ToList();

        var subjectKpis = rows
            .GroupBy(x => new { x.AsignaturaId, x.Asignatura, x.Curso })
            .Select(g => new
            {
                g.Key.AsignaturaId,
                g.Key.Asignatura,
                g.Key.Curso,
                Media = g.Average(x => x.NotaFinal!.Value),
                TotalAlumnos = g.Count(),
                Aprobados = g.Count(x => x.NotaFinal >= 5),
                Suspensos = g.Count(x => x.NotaFinal < 5)
            })
            .ToList();

        return new AdminTop5StatsDto
        {
            TopCursos = courseKpis
                .OrderByDescending(x => x.Media)
                .Take(5)
                .Select(x => new CursoResumenKpiDto
                {
                    Curso = x.Curso,
                    MediaGlobalCurso = Math.Round(x.Media, 2),
                    PorcentajeAprobados = ToPercentage(x.Aprobados, x.TotalAlumnos),
                    PorcentajeSuspensos = ToPercentage(x.Suspensos, x.TotalAlumnos)
                })
                .ToList(),

            BottomCursos = courseKpis
                .OrderBy(x => x.Media)
                .Take(5)
                .Select(x => new CursoResumenKpiDto
                {
                    Curso = x.Curso,
                    MediaGlobalCurso = Math.Round(x.Media, 2),
                    PorcentajeAprobados = ToPercentage(x.Aprobados, x.TotalAlumnos),
                    PorcentajeSuspensos = ToPercentage(x.Suspensos, x.TotalAlumnos)
                })
                .ToList(),

            TopAsignaturas = subjectKpis
                .OrderByDescending(x => x.Media)
                .Take(5)
                .Select(x => new AsignaturaTop5ItemDto
                {
                    AsignaturaId = x.AsignaturaId,
                    Asignatura = x.Asignatura,
                    Curso = x.Curso,
                    Media = Math.Round(x.Media, 2),
                    PorcentajeAprobados = ToPercentage(x.Aprobados, x.TotalAlumnos),
                    PorcentajeSuspensos = ToPercentage(x.Suspensos, x.TotalAlumnos)
                })
                .ToList(),

            BottomAsignaturas = subjectKpis
                .OrderBy(x => x.Media)
                .Take(5)
                .Select(x => new AsignaturaTop5ItemDto
                {
                    AsignaturaId = x.AsignaturaId,
                    Asignatura = x.Asignatura,
                    Curso = x.Curso,
                    Media = Math.Round(x.Media, 2),
                    PorcentajeAprobados = ToPercentage(x.Aprobados, x.TotalAlumnos),
                    PorcentajeSuspensos = ToPercentage(x.Suspensos, x.TotalAlumnos)
                })
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
            .Where(c => c.Id == cursoId)
            .Select(c => new { c.Id, c.Nombre })
            .FirstOrDefaultAsync(cancellationToken);

        if (curso is null)
            return null;

        // 🔥 MATERIALIZACIÓN CLAVE
        var rows = await BuildEnrollmentFinalRowsQuery(context)
            .Where(x => x.CursoId == cursoId)
            .ToListAsync(cancellationToken);

        // 🔥 AGRUPACIÓN ASIGNATURAS
        var asignaturasRaw = rows
            .GroupBy(x => new { x.AsignaturaId, x.Asignatura })
            .Select(g =>
            {
                var conNota = g.Where(x => x.NotaFinal.HasValue).ToList();

                return new
                {
                    g.Key.AsignaturaId,
                    g.Key.Asignatura,
                    TotalAlumnos = g.Count(),
                    Aprobados = conNota.Count(x => x.NotaFinal >= 5),
                    Suspensos = conNota.Count(x => x.NotaFinal < 5),
                    SinNota = g.Count(x => !x.NotaFinal.HasValue),
                    Media = conNota.Count > 0 ? conNota.Average(x => x.NotaFinal) : null
                };
            })
            .OrderBy(x => x.Asignatura)
            .ToList();

        var conNotaCurso = rows.Where(x => x.NotaFinal.HasValue).ToList();

        var totalAlumnos = rows.Count;
        var aprobados = conNotaCurso.Count(x => x.NotaFinal >= 5);
        var suspensos = conNotaCurso.Count(x => x.NotaFinal < 5);
        var sinNota = totalAlumnos - aprobados - suspensos;

        var mediaCurso = conNotaCurso.Count > 0
            ? conNotaCurso.Average(x => x.NotaFinal)
            : null;

        var asignaturas = asignaturasRaw.Select(x => new AsignaturaNotasStatsDto
        {
            AsignaturaId = x.AsignaturaId,
            Asignatura = x.Asignatura,
            TotalAlumnos = x.TotalAlumnos,
            Aprobados = x.Aprobados,
            Suspensos = x.Suspensos,
            SinNota = x.SinNota,
            Media = x.Media.HasValue ? Math.Round(x.Media.Value, 2) : null,
            PorcentajeAprobados = ToPercentage(x.Aprobados, x.TotalAlumnos),
            PorcentajeSuspensos = ToPercentage(x.Suspensos, x.TotalAlumnos)
        }).ToList();

        return new CursoNotasStatsResponseDto
        {
            CursoId = curso.Id,
            Curso = curso.Nombre,
            MediaGlobalCurso = mediaCurso.HasValue ? Math.Round(mediaCurso.Value, 2) : null,
            TotalAlumnos = totalAlumnos,
            Aprobados = aprobados,
            Suspensos = suspensos,
            SinNota = sinNota,
            PorcentajeAprobados = ToPercentage(aprobados, totalAlumnos),
            PorcentajeSuspensos = ToPercentage(suspensos, totalAlumnos),

            AsignaturaConMejorMedia = asignaturas
                .Where(x => x.Media.HasValue)
                .OrderByDescending(x => x.Media)
                .FirstOrDefault(),

            AsignaturaConPeorMedia = asignaturas
                .Where(x => x.Media.HasValue)
                .OrderBy(x => x.Media)
                .FirstOrDefault(),

            AsignaturaDestacada = asignaturas
                .OrderByDescending(x => x.PorcentajeAprobados)
                .ThenByDescending(x => x.Media)
                .FirstOrDefault(),

            AsignaturaAVigilar = asignaturas
                .OrderByDescending(x => x.PorcentajeSuspensos)
                .ThenBy(x => x.Media)
                .FirstOrDefault(),

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

    private static IQueryable<EnrollmentFinalRow> BuildEnrollmentFinalRowsQuery(AppDbContext appDbContext)
    {
        var finalGrades = BuildFinalGradesQuery(appDbContext);

        return
            from ea in appDbContext.EstudianteAsignaturas.AsNoTracking()
            join a in appDbContext.Asignaturas on ea.AsignaturaId equals a.Id
            join c in appDbContext.Cursos on a.CursoId equals c.Id
            join fg in finalGrades
                on new { ea.EstudianteId, ea.AsignaturaId }
                equals new { fg.EstudianteId, fg.AsignaturaId }
                into fgJoin
            from fg in fgJoin.DefaultIfEmpty()
            select new EnrollmentFinalRow
            {
                EstudianteId = ea.EstudianteId,
                CursoId = c.Id,
                Curso = c.Nombre,
                AsignaturaId = a.Id,
                Asignatura = a.Nombre,
                NotaFinal = fg != null ? fg.NotaFinal : null
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
