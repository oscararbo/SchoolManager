using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Persistence.Context;
using Back.Api.Application.Dtos;
using Back.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Back.Api.Persistence.Repositories;

public class AdminDomainRepository(AppDbContext context, IDbContextFactory<AppDbContext> contextFactory) : IAdminDomainRepository
{
    private sealed class AdminTotalsSnapshot
    {
        public int TotalCursos { get; set; }
        public int TotalAsignaturas { get; set; }
        public int TotalProfesores { get; set; }
        public int TotalEstudiantes { get; set; }
        public int TotalMatriculas { get; set; }
        public int TotalTareas { get; set; }
    }

    public async Task<IEnumerable<AdminListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => await context.Admins
            .AsNoTracking()
            .Select(a => new AdminListItemDto { Id = a.Id, Nombre = a.Nombre, Correo = a.Cuenta!.Correo })
            .ToListAsync(cancellationToken);

    public async Task<AdminStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var totals = await context.Database
            .SqlQueryRaw<AdminTotalsSnapshot>(
                """
                SELECT
                    (SELECT COUNT(*) FROM \"Cursos\" c WHERE c.\"IsDeleted\" = FALSE) AS \"TotalCursos\",
                    (SELECT COUNT(*) FROM \"Asignaturas\" a WHERE a.\"IsDeleted\" = FALSE) AS \"TotalAsignaturas\",
                    (SELECT COUNT(*) FROM \"Profesores\" p WHERE p.\"IsDeleted\" = FALSE) AS \"TotalProfesores\",
                    (SELECT COUNT(*) FROM \"Estudiantes\" e WHERE e.\"IsDeleted\" = FALSE) AS \"TotalEstudiantes\",
                    (SELECT COUNT(*) FROM \"EstudianteAsignaturas\" ea WHERE ea.\"IsDeleted\" = FALSE) AS \"TotalMatriculas\",
                    (SELECT COUNT(*) FROM \"Tareas\" t WHERE t.\"IsDeleted\" = FALSE) AS \"TotalTareas\"
                """)
            .SingleAsync(cancellationToken);

        var estudiantesPorCurso = await context.Estudiantes
            .AsNoTracking()
            .GroupBy(e => e.CursoId)
            .Select(g => new { CursoId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var asignaturasPorCurso = await context.Asignaturas
            .AsNoTracking()
            .GroupBy(a => a.CursoId)
            .Select(g => new { CursoId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var estudiantesMap = estudiantesPorCurso.ToDictionary(x => x.CursoId, x => x.Count);
        var asignaturasMap = asignaturasPorCurso.ToDictionary(x => x.CursoId, x => x.Count);

        var cursos = await context.Cursos
            .AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new { c.Id, c.Nombre })
            .ToListAsync(cancellationToken);

        var porCurso = cursos.Select(c => new CursoStatsItemDto
        {
            Curso = c.Nombre,
            Estudiantes = estudiantesMap.GetValueOrDefault(c.Id),
            Asignaturas = asignaturasMap.GetValueOrDefault(c.Id)
        }).ToList();

        return new AdminStatsDto
        {
            TotalCursos = totals.TotalCursos,
            TotalAsignaturas = totals.TotalAsignaturas,
            TotalProfesores = totals.TotalProfesores,
            TotalEstudiantes = totals.TotalEstudiantes,
            TotalMatriculas = totals.TotalMatriculas,
            TotalTareas = totals.TotalTareas,
            PorCurso = porCurso
        };
    }

    public async Task<IEnumerable<CursoStatsSelectorDto>> GetCursosStatsSelectorAsync(CancellationToken cancellationToken = default)
    {
        var estudiantesPorCurso = await context.Estudiantes
            .AsNoTracking()
            .GroupBy(e => e.CursoId)
            .Select(g => new { CursoId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var asignaturasPorCurso = await context.Asignaturas
            .AsNoTracking()
            .GroupBy(a => a.CursoId)
            .Select(g => new { CursoId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var estudiantesMap = estudiantesPorCurso.ToDictionary(x => x.CursoId, x => x.Count);
        var asignaturasMap = asignaturasPorCurso.ToDictionary(x => x.CursoId, x => x.Count);

        var cursos = await context.Cursos
            .AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new { c.Id, c.Nombre })
            .ToListAsync(cancellationToken);

        return cursos.Select(c => new CursoStatsSelectorDto
        {
            CursoId = c.Id,
            Curso = c.Nombre,
            TotalEstudiantes = estudiantesMap.GetValueOrDefault(c.Id),
            TotalAsignaturas = asignaturasMap.GetValueOrDefault(c.Id)
        }).ToList();
    }

    public Task<CursoNotasStatsResponseDto?> GetStatsByCursoAsync(int cursoId, CancellationToken cancellationToken = default)
        => BuildCursoStatsAsync(cursoId, cancellationToken);

    public async Task<IEnumerable<CursoComparacionItemDto>> CompareCursosAsync(IEnumerable<int> cursoIds, CancellationToken cancellationToken = default)
    {
        var ids = cursoIds.Where(id => id > 0).Distinct().ToList();
        var statsTasks = ids.Select(id => BuildCursoStatsWithFactoryAsync(id, cancellationToken));
        var statsByCurso = await Task.WhenAll(statsTasks);

        return statsByCurso
            .Where(stats => stats is not null)
            .Select(stats => new CursoComparacionItemDto
            {
                CursoId = stats!.CursoId,
                Curso = stats.Curso,
                MediaGlobalCurso = stats.MediaGlobalCurso,
                TotalAlumnos = stats.TotalAlumnos,
                Aprobados = stats.Aprobados,
                Suspensos = stats.Suspensos,
                SinNota = stats.SinNota
            })
            .OrderBy(x => x.Curso)
            .ToList();
    }

    public Task<bool> CorreoDuplicadoAsync(string correo, CancellationToken cancellationToken = default)
        => context.Cuentas.AnyAsync(c => c.Correo == correo, cancellationToken);

    public async Task<AdminListItemDto> CreateAsync(string nombre, string correo, string hash, CancellationToken cancellationToken = default)
    {
        var admin = new Admin
        {
            Nombre = nombre,
            Cuenta = new Cuenta
            {
                Correo = correo,
                Contrasena = hash,
                Rol = "admin"
            }
        };
        context.Admins.Add(admin);
        await context.SaveChangesAsync(cancellationToken);
        return new AdminListItemDto { Id = admin.Id, Nombre = admin.Nombre, Correo = admin.Cuenta!.Correo };
    }

    public async Task<IEnumerable<AdminMatriculaListReadModelDto>> GetMatriculasAsync(CancellationToken cancellationToken = default)
        => await context.Estudiantes
            .AsNoTracking()
            .Include(e => e.EstudianteAsignaturas)
                .ThenInclude(ea => ea.Asignatura)
            .Include(e => e.Curso)
            .OrderBy(e => e.Nombre)
            .Select(e => new AdminMatriculaListReadModelDto
            {
                EstudianteId = e.Id,
                Estudiante = e.Nombre,
                CursoId = e.CursoId,
                Curso = e.Curso != null ? e.Curso.Nombre : null,
                Asignaturas = e.EstudianteAsignaturas
                    .Where(ea => !ea.IsDeleted)
                    .Select(ea => new AdminMatriculaAsignaturaReadModelDto
                    {
                        AsignaturaId = ea.AsignaturaId,
                        Asignatura = ea.Asignatura != null ? ea.Asignatura.Nombre : string.Empty
                    })
                    .OrderBy(a => a.Asignatura)
                    .ToList()
            })
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<AdminImparticionListReadModelDto>> GetImparticionesAsync(CancellationToken cancellationToken = default)
        => await context.ProfesorAsignaturaCursos
            .AsNoTracking()
            .OrderBy(x => x.Curso!.Nombre)
            .ThenBy(x => x.Asignatura!.Nombre)
            .Select(x => new AdminImparticionListReadModelDto
            {
                ProfesorId = x.ProfesorId,
                Profesor = x.Profesor != null ? x.Profesor.Nombre : string.Empty,
                AsignaturaId = x.AsignaturaId,
                Asignatura = x.Asignatura != null ? x.Asignatura.Nombre : string.Empty,
                CursoId = x.CursoId,
                Curso = x.Curso != null ? x.Curso.Nombre : string.Empty
            })
            .ToListAsync(cancellationToken);

    private async Task<CursoNotasStatsResponseDto?> BuildCursoStatsWithFactoryAsync(int cursoId, CancellationToken cancellationToken)
    {
        await using var isolatedContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await BuildCursoStatsAsync(isolatedContext, cursoId, cancellationToken);
    }

    private Task<CursoNotasStatsResponseDto?> BuildCursoStatsAsync(int cursoId, CancellationToken cancellationToken)
        => BuildCursoStatsAsync(context, cursoId, cancellationToken);

    private static async Task<CursoNotasStatsResponseDto?> BuildCursoStatsAsync(AppDbContext dbContext, int cursoId, CancellationToken cancellationToken)
    {
        var curso = await dbContext.Cursos
            .AsNoTracking()
            .Where(c => c.Id == cursoId)
            .Select(c => new { c.Id, c.Nombre })
            .FirstOrDefaultAsync(cancellationToken);

        if (curso is null)
            return null;

        var asignaturas = await dbContext.Asignaturas
            .AsNoTracking()
            .Where(a => a.CursoId == cursoId)
            .OrderBy(a => a.Nombre)
            .Select(a => new { a.Id, a.Nombre })
            .ToListAsync(cancellationToken);

        var asignaturaIds = asignaturas.Select(a => a.Id).ToList();
        var tareas = await dbContext.Tareas
            .AsNoTracking()
            .Where(t => asignaturaIds.Contains(t.AsignaturaId))
            .Select(t => new { t.Id, t.AsignaturaId, t.Trimestre })
            .ToListAsync(cancellationToken);

        var tareaIds = tareas.Select(t => t.Id).ToList();
        var matriculas = await dbContext.EstudianteAsignaturas
            .AsNoTracking()
            .Where(ea => asignaturaIds.Contains(ea.AsignaturaId))
            .Select(ea => new { ea.AsignaturaId, ea.EstudianteId })
            .ToListAsync(cancellationToken);

        var notas = await dbContext.Notas
            .AsNoTracking()
            .Where(n => tareaIds.Contains(n.TareaId))
            .Select(n => new { n.EstudianteId, n.TareaId, Valor = (double)n.Valor })
            .ToListAsync(cancellationToken);

        var notaMap = notas.ToDictionary(n => (n.EstudianteId, n.TareaId), n => (double?)n.Valor);
        var asignaturasStats = new List<AsignaturaNotasStatsDto>();
        var acumuladoFinales = new List<double>();
        var totalAlumnos = 0;
        var aprobados = 0;
        var suspensos = 0;
        var sinNota = 0;

        foreach (var asignatura in asignaturas)
        {
            var tareasAsignatura = tareas.Where(t => t.AsignaturaId == asignatura.Id).ToList();
            var estudianteIds = matriculas
                .Where(m => m.AsignaturaId == asignatura.Id)
                .Select(m => m.EstudianteId)
                .Distinct()
                .ToList();

            var finales = estudianteIds
                .Select(estudianteId => CalcularNotaFinal(tareasAsignatura, estudianteId, notaMap))
                .ToList();

            var finalesValidas = finales.Where(f => f.HasValue).Select(f => f!.Value).ToList();
            var aprobadosAsignatura = finalesValidas.Count(f => f >= 5);
            var suspensosAsignatura = finalesValidas.Count(f => f < 5);
            var sinNotaAsignatura = finales.Count - finalesValidas.Count;

            totalAlumnos += finales.Count;
            aprobados += aprobadosAsignatura;
            suspensos += suspensosAsignatura;
            sinNota += sinNotaAsignatura;
            acumuladoFinales.AddRange(finalesValidas);

            asignaturasStats.Add(new AsignaturaNotasStatsDto
            {
                AsignaturaId = asignatura.Id,
                Asignatura = asignatura.Nombre,
                TotalAlumnos = finales.Count,
                Aprobados = aprobadosAsignatura,
                Suspensos = suspensosAsignatura,
                SinNota = sinNotaAsignatura,
                Media = finalesValidas.Count > 0 ? Math.Round(finalesValidas.Average(), 2) : null
            });
        }

        return new CursoNotasStatsResponseDto
        {
            CursoId = curso.Id,
            Curso = curso.Nombre,
            MediaGlobalCurso = acumuladoFinales.Count > 0 ? Math.Round(acumuladoFinales.Average(), 2) : null,
            TotalAlumnos = totalAlumnos,
            Aprobados = aprobados,
            Suspensos = suspensos,
            SinNota = sinNota,
            Asignaturas = asignaturasStats
        };
    }

    private static double? CalcularNotaFinal(
        IEnumerable<dynamic> tareasAsignatura,
        int estudianteId,
        IReadOnlyDictionary<(int EstudianteId, int TareaId), double?> notaMap)
    {
        double? MediaTrimestre(int trimestre)
        {
            var tareaIds = tareasAsignatura
                .Where(t => t.Trimestre == trimestre)
                .Select(t => (int)t.Id)
                .ToList();

            var valores = tareaIds
                .Select(tareaId => notaMap.TryGetValue((estudianteId, tareaId), out var valor) ? valor : null)
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();

            return valores.Count > 0 ? valores.Average() : null;
        }

        var t1 = MediaTrimestre(1);
        var t2 = MediaTrimestre(2);
        var t3 = MediaTrimestre(3);

        return (t1.HasValue && t2.HasValue && t3.HasValue)
            ? Math.Round((t1.Value + t2.Value + t3.Value) / 3, 2)
            : null;
    }
}
