using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Persistence.Context;
using Back.Api.Application.Dtos;
using Back.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Back.Api.Persistence.Repositories;

public class AdminDomainRepository(AppDbContext context) : IAdminDomainRepository
{
    public async Task<IEnumerable<AdminListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => await context.Admins
            .AsNoTracking()
            .Select(a => new AdminListItemDto { Id = a.Id, Nombre = a.Nombre, Correo = a.Correo })
            .ToListAsync(cancellationToken);

    public async Task<AdminStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var totalCursos = await context.Cursos.CountAsync(cancellationToken);
        var totalAsignaturas = await context.Asignaturas.CountAsync(cancellationToken);
        var totalProfesores = await context.Profesores.CountAsync(cancellationToken);
        var totalEstudiantes = await context.Estudiantes.CountAsync(cancellationToken);
        var totalMatriculas = await context.EstudianteAsignaturas.CountAsync(cancellationToken);
        var totalTareas = await context.Tareas.CountAsync(cancellationToken);

        var porCurso = await context.Cursos
            .AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new CursoStatsItemDto
            {
                Curso = c.Nombre,
                Estudiantes = context.Estudiantes.Count(e => e.CursoId == c.Id),
                Asignaturas = context.Asignaturas.Count(a => a.CursoId == c.Id)
            })
            .ToListAsync(cancellationToken);

        return new AdminStatsDto
        {
            TotalCursos = totalCursos,
            TotalAsignaturas = totalAsignaturas,
            TotalProfesores = totalProfesores,
            TotalEstudiantes = totalEstudiantes,
            TotalMatriculas = totalMatriculas,
            TotalTareas = totalTareas,
            PorCurso = porCurso
        };
    }

    public async Task<IEnumerable<CursoStatsSelectorDto>> GetCursosStatsSelectorAsync(CancellationToken cancellationToken = default)
        => await context.Cursos
            .AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new CursoStatsSelectorDto
            {
                CursoId = c.Id,
                Curso = c.Nombre,
                TotalEstudiantes = context.Estudiantes.Count(e => e.CursoId == c.Id),
                TotalAsignaturas = context.Asignaturas.Count(a => a.CursoId == c.Id)
            })
            .ToListAsync(cancellationToken);

    public Task<CursoNotasStatsResponseDto?> GetStatsByCursoAsync(int cursoId, CancellationToken cancellationToken = default)
        => BuildCursoStatsAsync(cursoId, cancellationToken);

    public async Task<IEnumerable<CursoComparacionItemDto>> CompareCursosAsync(IEnumerable<int> cursoIds, CancellationToken cancellationToken = default)
    {
        var result = new List<CursoComparacionItemDto>();

        foreach (var cursoId in cursoIds)
        {
            var stats = await BuildCursoStatsAsync(cursoId, cancellationToken);
            if (stats is null)
                continue;

            result.Add(new CursoComparacionItemDto
            {
                CursoId = stats.CursoId,
                Curso = stats.Curso,
                MediaGlobalCurso = stats.MediaGlobalCurso,
                TotalAlumnos = stats.TotalAlumnos,
                Aprobados = stats.Aprobados,
                Suspensos = stats.Suspensos,
                SinNota = stats.SinNota
            });
        }

        return result.OrderBy(x => x.Curso).ToList();
    }

    public Task<bool> CorreoDuplicadoAsync(string correo, CancellationToken cancellationToken = default)
        => context.Admins.AnyAsync(a => a.Correo == correo);

    public async Task<AdminListItemDto> CreateAsync(string nombre, string correo, string hash, CancellationToken cancellationToken = default)
    {
        var admin = new Admin { Nombre = nombre, Correo = correo, Contrasena = hash };
        context.Admins.Add(admin);
        await context.SaveChangesAsync();
        return new AdminListItemDto { Id = admin.Id, Nombre = admin.Nombre, Correo = admin.Correo };
    }

    public async Task<IEnumerable<AdminMatriculaListReadModelDto>> GetMatriculasAsync(CancellationToken cancellationToken = default)
        => await context.Estudiantes
            .AsNoTracking()
            .OrderBy(e => e.Nombre)
            .Select(e => new AdminMatriculaListReadModelDto
            {
                EstudianteId = e.Id,
                Estudiante = e.Nombre,
                CursoId = e.CursoId,
                Curso = e.Curso != null ? e.Curso.Nombre : null,
                Asignaturas = context.EstudianteAsignaturas
                    .Where(ea => ea.EstudianteId == e.Id)
                    .Join(context.Asignaturas,
                        ea => ea.AsignaturaId,
                        a => a.Id,
                        (_, a) => new AdminMatriculaAsignaturaReadModelDto
                        {
                            AsignaturaId = a.Id,
                            Asignatura = a.Nombre
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

    private async Task<CursoNotasStatsResponseDto?> BuildCursoStatsAsync(int cursoId, CancellationToken cancellationToken)
    {
        var curso = await context.Cursos
            .AsNoTracking()
            .Where(c => c.Id == cursoId)
            .Select(c => new { c.Id, c.Nombre })
            .FirstOrDefaultAsync(cancellationToken);

        if (curso is null)
            return null;

        var asignaturas = await context.Asignaturas
            .AsNoTracking()
            .Where(a => a.CursoId == cursoId)
            .OrderBy(a => a.Nombre)
            .Select(a => new { a.Id, a.Nombre })
            .ToListAsync(cancellationToken);

        var asignaturaIds = asignaturas.Select(a => a.Id).ToList();
        var tareas = await context.Tareas
            .AsNoTracking()
            .Where(t => asignaturaIds.Contains(t.AsignaturaId))
            .Select(t => new { t.Id, t.AsignaturaId, t.Trimestre })
            .ToListAsync(cancellationToken);

        var tareaIds = tareas.Select(t => t.Id).ToList();
        var matriculas = await context.EstudianteAsignaturas
            .AsNoTracking()
            .Where(ea => asignaturaIds.Contains(ea.AsignaturaId))
            .Select(ea => new { ea.AsignaturaId, ea.EstudianteId })
            .ToListAsync(cancellationToken);

        var notas = await context.Notas
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
