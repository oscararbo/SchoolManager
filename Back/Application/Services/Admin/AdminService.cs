using Back.Api.Application.Common;
using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Abstractions.Security;
using Back.Api.Application.Dtos;
using System.Security.Claims;

namespace Back.Api.Application.Services;

public class AdminService(
    IAdminDomainRepository adminDomain,
    ICursosDomainRepository cursosDomain,
    IAsignaturasDomainRepository asignaturasDomain,
    IProfesoresDomainRepository profesoresDomain,
    IEstudiantesDomainRepository estudiantesDomain,
    IPasswordService passwordService) : IAdminService
{
    public async Task<ApplicationResult> GetAllAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await adminDomain.GetAllAsync(cancellationToken));

    public async Task<ApplicationResult> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var cursos = (await cursosDomain.GetAllResumenAsync(cancellationToken)).ToList();
        var asignaturas = (await asignaturasDomain.GetAllResumenAsync(cancellationToken)).ToList();
        var profesores = (await profesoresDomain.GetAllAsync(cancellationToken)).ToList();
        var estudiantes = (await estudiantesDomain.GetAllAsync(cancellationToken)).ToList();

        var totalTareas = 0;
        foreach (var asignatura in asignaturas)
        {
            totalTareas += (await profesoresDomain.GetTareasConNotasAsync(asignatura.Id, cancellationToken)).Count();
        }

        var porCurso = cursos
            .OrderBy(c => c.Nombre)
            .Select(curso => new CursoStatsItemDto
            {
                Curso = curso.Nombre,
                Estudiantes = estudiantes.Count(e => e.CursoId == curso.Id),
                Asignaturas = curso.Asignaturas.Count
            })
            .ToList();

        return ApplicationResult.Ok(new AdminStatsDto
        {
            TotalCursos = cursos.Count,
            TotalAsignaturas = asignaturas.Count,
            TotalProfesores = profesores.Count,
            TotalEstudiantes = estudiantes.Count,
            TotalMatriculas = asignaturas.Sum(a => a.Alumnos.Count),
            TotalTareas = totalTareas,
            PorCurso = porCurso
        });
    }

    public async Task<ApplicationResult> GetCursosStatsSelectorAsync(CancellationToken cancellationToken = default)
    {
        var cursos = (await cursosDomain.GetAllResumenAsync(cancellationToken)).ToList();
        var estudiantes = (await estudiantesDomain.GetAllAsync(cancellationToken)).ToList();

        var result = cursos
            .OrderBy(c => c.Nombre)
            .Select(c => new CursoStatsSelectorDto
            {
                CursoId = c.Id,
                Curso = c.Nombre,
                TotalEstudiantes = estudiantes.Count(e => e.CursoId == c.Id),
                TotalAsignaturas = c.Asignaturas.Count
            })
            .ToList();

        return ApplicationResult.Ok(result);
    }

    public async Task<ApplicationResult> GetStatsByCursoAsync(int cursoId, CancellationToken cancellationToken = default)
    {
        var curso = await cursosDomain.GetSimpleAsync(cursoId, cancellationToken);
        if (curso is null)
            return ApplicationResult.NotFound("El curso no existe.");

        var result = await ConstruirStatsCursoAsync(curso.Id, curso.Nombre, cancellationToken);
        return ApplicationResult.Ok(result);
    }

    public async Task<ApplicationResult> CompareCursosAsync(IEnumerable<int> cursoIds, CancellationToken cancellationToken = default)
    {
        var ids = cursoIds
            .Where(id => id > 0)
            .Distinct()
            .Take(6)
            .ToList();

        if (ids.Count < 2)
            return ApplicationResult.BadRequest("Selecciona al menos 2 cursos para comparar.");

        var result = new List<CursoComparacionItemDto>();
        foreach (var id in ids)
        {
            var curso = await cursosDomain.GetSimpleAsync(id, cancellationToken);
            if (curso is null)
            {
                continue;
            }

            var stats = await ConstruirStatsCursoAsync(curso.Id, curso.Nombre, cancellationToken);
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

        return ApplicationResult.Ok(new ComparacionCursosResponseDto
        {
            Cursos = result.OrderBy(r => r.Curso).ToList()
        });
    }

    public async Task<ApplicationResult> GetMatriculasAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await adminDomain.GetMatriculasAsync(cancellationToken));

    public async Task<ApplicationResult> GetImparticionesAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await adminDomain.GetImparticionesAsync(cancellationToken));

    public async Task<ApplicationResult> CreateAsync(CreateAdminDto dto, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!user.IsInRole("admin"))
            return ApplicationResult.Forbidden();
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return ApplicationResult.BadRequest("El nombre del administrador es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Correo))
            return ApplicationResult.BadRequest("El correo del administrador es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Contrasena))
            return ApplicationResult.BadRequest("La contrasena del administrador es obligatoria.");

        var correo = dto.Correo.Trim().ToLowerInvariant();
        if (await adminDomain.CorreoDuplicadoAsync(correo, cancellationToken))
            return ApplicationResult.BadRequest("Ya existe un administrador con ese correo.");

        var result = await adminDomain.CreateAsync(dto.Nombre.Trim(), correo, passwordService.Hash(dto.Contrasena.Trim()), cancellationToken);
        return ApplicationResult.Created($"/api/admin/{result.Id}", result);
    }

    private static double? CalcularNotaFinal(IEnumerable<AsignaturaNotaSimpleDto> notas)
    {
        double? MediaTrimestre(int trimestre)
        {
            var valores = notas
                .Where(n => n.Trimestre == trimestre)
                .Select(n => (double)n.Valor)
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

    private async Task<CursoNotasStatsResponseDto> ConstruirStatsCursoAsync(int cursoId, string cursoNombre, CancellationToken cancellationToken)
    {
        var asignaturasCurso = (await asignaturasDomain.GetAllResumenAsync(cancellationToken))
            .Where(a => a.Curso.Id == cursoId)
            .OrderBy(a => a.Nombre)
            .ToList();

        var asignaturasStats = new List<AsignaturaNotasStatsDto>();
        var acumuladoFinales = new List<double>();
        var totalAlumnos = 0;
        var aprobados = 0;
        var suspensos = 0;
        var sinNota = 0;

        foreach (var asignatura in asignaturasCurso)
        {
            var detalle = await asignaturasDomain.GetDetalleAsync(asignatura.Id, cancellationToken);
            if (detalle is null)
            {
                continue;
            }

            var finales = detalle.Alumnos
                .Select(alumno => CalcularNotaFinal(alumno.Notas))
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
                AsignaturaId = detalle.Id,
                Asignatura = detalle.Nombre,
                TotalAlumnos = finales.Count,
                Aprobados = aprobadosAsignatura,
                Suspensos = suspensosAsignatura,
                SinNota = sinNotaAsignatura,
                Media = finalesValidas.Count > 0 ? Math.Round(finalesValidas.Average(), 2) : null
            });
        }

        return new CursoNotasStatsResponseDto
        {
            CursoId = cursoId,
            Curso = cursoNombre,
            MediaGlobalCurso = acumuladoFinales.Count > 0 ? Math.Round(acumuladoFinales.Average(), 2) : null,
            TotalAlumnos = totalAlumnos,
            Aprobados = aprobados,
            Suspensos = suspensos,
            SinNota = sinNota,
            Asignaturas = asignaturasStats
        };
    }
}
