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

    public async Task<ApplicationResult> GetNotasStatsAsync(CancellationToken cancellationToken = default)
    {
        var cursos = (await cursosDomain.GetAllResumenAsync(cancellationToken))
            .OrderBy(c => c.Nombre)
            .ToList();

        var asignaturas = (await asignaturasDomain.GetAllResumenAsync(cancellationToken)).ToList();
        var cursosStats = new List<CursoNotasStatsDto>();
        var mediasGlobales = new List<double>();

        foreach (var curso in cursos)
        {
            var asignaturasCurso = asignaturas
                .Where(a => a.Curso.Id == curso.Id)
                .OrderBy(a => a.Nombre)
                .ToList();

            var asignaturasStats = new List<AsignaturaNotasStatsDto>();

            foreach (var asignatura in asignaturasCurso)
            {
                var detalle = await asignaturasDomain.GetDetalleAsync(asignatura.Id, cancellationToken);
                if (detalle is null)
                {
                    continue;
                }

                var alumnos = detalle.Alumnos
                    .Select(alumno =>
                    {
                        var notaFinal = CalcularNotaFinal(alumno.Notas);
                        return new AlumnoNotaResumenDto
                        {
                            EstudianteId = alumno.EstudianteId,
                            Estudiante = alumno.Alumno,
                            NotaFinal = notaFinal,
                            Aprobado = notaFinal.HasValue && notaFinal.Value >= 5
                        };
                    })
                    .OrderBy(a => a.Estudiante)
                    .ToList();

                var mediasAsignatura = alumnos.Where(a => a.NotaFinal.HasValue).Select(a => a.NotaFinal!.Value).ToList();
                mediasGlobales.AddRange(mediasAsignatura);

                asignaturasStats.Add(new AsignaturaNotasStatsDto
                {
                    AsignaturaId = detalle.Id,
                    Asignatura = detalle.Nombre,
                    TotalAlumnos = alumnos.Count,
                    Aprobados = alumnos.Count(a => a.Aprobado),
                    Suspensos = alumnos.Count(a => a.NotaFinal.HasValue && !a.Aprobado),
                    SinNota = alumnos.Count(a => !a.NotaFinal.HasValue),
                    Media = mediasAsignatura.Count > 0 ? Math.Round(mediasAsignatura.Average(), 2) : null,
                    Alumnos = alumnos
                });
            }

            var mediasCurso = asignaturasStats
                .SelectMany(a => a.Alumnos)
                .Where(a => a.NotaFinal.HasValue)
                .Select(a => a.NotaFinal!.Value)
                .ToList();

            cursosStats.Add(new CursoNotasStatsDto
            {
                CursoId = curso.Id,
                Curso = curso.Nombre,
                Media = mediasCurso.Count > 0 ? Math.Round(mediasCurso.Average(), 2) : null,
                Asignaturas = asignaturasStats
            });
        }

        return ApplicationResult.Ok(new AdminNotasStatsDto
        {
            MediaGlobal = mediasGlobales.Count > 0 ? Math.Round(mediasGlobales.Average(), 2) : null,
            PorCurso = cursosStats
        });
    }

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
}
