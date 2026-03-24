using Back.Api.Application.Common;
using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Abstractions.Security;
using Back.Api.Application.Dtos;
using System.Security.Claims;

namespace Back.Api.Application.Services;

public class ProfesoresService(IProfesoresDomainRepository profesoresDomain, IPasswordService passwordService) : IProfesoresService
{
    public async Task<ApplicationResult> GetAllAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await profesoresDomain.GetAllAsync(cancellationToken));

    public async Task<ApplicationResult> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var profesor = await profesoresDomain.GetDetalleAsync(id, cancellationToken);
        return profesor is null
            ? ApplicationResult.NotFound("El profesor no existe.")
            : ApplicationResult.Ok(profesor);
    }

    public async Task<ApplicationResult> CreateAsync(CreateProfesorDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return ApplicationResult.BadRequest("El nombre del profesor es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Correo))
            return ApplicationResult.BadRequest("El correo del profesor es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Contrasena))
            return ApplicationResult.BadRequest("La contrasena del profesor es obligatoria.");

        var correo = dto.Correo.Trim().ToLowerInvariant();
        if (await profesoresDomain.CorreoDuplicadoAsync(correo, cancellationToken))
            return ApplicationResult.BadRequest("Ya existe un profesor con ese correo.");

        var result = await profesoresDomain.CreateAsync(dto.Nombre.Trim(), correo, passwordService.Hash(dto.Contrasena.Trim()), cancellationToken);
        return ApplicationResult.Created($"/api/profesores/{result.Id}", result);
    }

    public async Task<ApplicationResult> GetPanelProfesorAsync(int id, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConProfesor(id, user))
            return ApplicationResult.Forbidden();

        var panel = await profesoresDomain.GetPanelAsync(id, cancellationToken);
        return panel is null
            ? ApplicationResult.NotFound("El profesor no existe.")
            : ApplicationResult.Ok(panel);
    }

    public async Task<ApplicationResult> GetAlumnosDeAsignaturaAsync(int profesorId, int asignaturaId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();
        if (!await profesoresDomain.ProfesorExisteAsync(profesorId, cancellationToken))
            return ApplicationResult.NotFound("El profesor no existe.");
        if (!await profesoresDomain.ProfesorImparteAsignaturaAsync(profesorId, asignaturaId, cancellationToken))
            return ApplicationResult.BadRequest("El profesor no imparte esta asignatura.");

        var result = await profesoresDomain.GetAlumnosCompletoAsync(asignaturaId, cancellationToken);
        return result is null
            ? ApplicationResult.NotFound("La asignatura no existe.")
            : ApplicationResult.Ok(result);
    }

    public async Task<ApplicationResult> GetAlumnosResumenDeAsignaturaAsync(int profesorId, int asignaturaId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();
        if (!await profesoresDomain.ProfesorExisteAsync(profesorId, cancellationToken))
            return ApplicationResult.NotFound("El profesor no existe.");
        if (!await profesoresDomain.ProfesorImparteAsignaturaAsync(profesorId, asignaturaId, cancellationToken))
            return ApplicationResult.BadRequest("El profesor no imparte esta asignatura.");

        var asignaturaInfo = await profesoresDomain.GetAsignaturaInfoAsync(asignaturaId, cancellationToken);
        if (asignaturaInfo is null)
            return ApplicationResult.NotFound("La asignatura no existe.");

        var tareas = await profesoresDomain.GetTareasDeAsignaturaAsync(asignaturaId, cancellationToken);
        var alumnosBase = await profesoresDomain.GetAlumnosResumenAsync(asignaturaId, cancellationToken);

        var alumnos = new List<AsignaturaAlumnoResumenDto>();
        foreach (var alumno in alumnosBase)
        {
            var detalle = await profesoresDomain.GetAlumnoDetalleAsync(asignaturaId, alumno.EstudianteId, cancellationToken);
            if (detalle is null) continue;
            alumnos.Add(new AsignaturaAlumnoResumenDto
            {
                EstudianteId = detalle.EstudianteId,
                Alumno = detalle.Alumno,
                Medias = detalle.Medias,
                NotaFinal = detalle.NotaFinal
            });
        }

        return ApplicationResult.Ok(new AsignaturaAlumnosResumenResponseDto
        {
            Asignatura = asignaturaInfo,
            Tareas = tareas,
            Alumnos = alumnos
        });
    }

    public async Task<ApplicationResult> GetAlumnoDetalleDeAsignaturaAsync(int profesorId, int asignaturaId, int estudianteId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();
        if (!await profesoresDomain.ProfesorExisteAsync(profesorId, cancellationToken))
            return ApplicationResult.NotFound("El profesor no existe.");
        if (!await profesoresDomain.ProfesorImparteAsignaturaAsync(profesorId, asignaturaId, cancellationToken))
            return ApplicationResult.BadRequest("El profesor no imparte esta asignatura.");

        var detalle = await profesoresDomain.GetAlumnoDetalleAsync(asignaturaId, estudianteId, cancellationToken);
        return detalle is null
            ? ApplicationResult.NotFound("No se encontro el alumno en la asignatura indicada.")
            : ApplicationResult.Ok(detalle);
    }

    public async Task<ApplicationResult> GetCalificacionesDeTareaAsync(int profesorId, int asignaturaId, int tareaId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();
        if (!await profesoresDomain.ProfesorExisteAsync(profesorId, cancellationToken))
            return ApplicationResult.NotFound("El profesor no existe.");
        if (!await profesoresDomain.ProfesorImparteAsignaturaAsync(profesorId, asignaturaId, cancellationToken))
            return ApplicationResult.BadRequest("El profesor no imparte esta asignatura.");
        if (!await profesoresDomain.ProfesorImparteTareaAsync(profesorId, tareaId, cancellationToken))
            return ApplicationResult.BadRequest("El profesor no tiene acceso a esa tarea.");

        var tarea = await profesoresDomain.GetTareaResumenAsync(tareaId, cancellationToken);
        if (tarea is null)
            return ApplicationResult.NotFound("La tarea no existe.");

        var calificaciones = await profesoresDomain.GetCalificacionesTareaAsync(asignaturaId, tareaId, cancellationToken);
        return ApplicationResult.Ok(new AsignaturaCalificacionesTareaResponseDto
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
        });
    }

    public async Task<ApplicationResult> AsignarImparticionAsync(int profesorId, AsignarImparticionDto dto, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();
        if (!await profesoresDomain.ProfesorExisteAsync(profesorId, cancellationToken))
            return ApplicationResult.NotFound("El profesor no existe.");

        var asignatura = await profesoresDomain.GetAsignaturaBasicaAsync(dto.AsignaturaId, cancellationToken);
        if (asignatura is null)
            return ApplicationResult.NotFound("La asignatura no existe.");
        if (!await profesoresDomain.CursoExisteAsync(dto.CursoId, cancellationToken))
            return ApplicationResult.NotFound("El curso no existe.");
        if (asignatura.Value.CursoId != dto.CursoId)
            return ApplicationResult.BadRequest("La asignatura no pertenece a ese curso.");
        if (await profesoresDomain.AsignaturaYaTieneOtroProfesorAsync(dto.AsignaturaId, profesorId, cancellationToken))
            return ApplicationResult.BadRequest("La asignatura ya tiene un profesor asignado.");
        if (await profesoresDomain.ImparticionExisteAsync(profesorId, dto.AsignaturaId, dto.CursoId, cancellationToken))
            return ApplicationResult.BadRequest("La imparticion ya existe para ese profesor, asignatura y curso.");

        await profesoresDomain.AsignarImparticionAsync(profesorId, dto.AsignaturaId, dto.CursoId, cancellationToken);
        return ApplicationResult.Ok();
    }

    public async Task<ApplicationResult> EliminarImparticionAsync(int profesorId, int asignaturaId, int cursoId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();
        if (!await profesoresDomain.ProfesorExisteAsync(profesorId, cancellationToken))
            return ApplicationResult.NotFound("El profesor no existe.");
        if (!await profesoresDomain.ImparticionExisteAsync(profesorId, asignaturaId, cursoId, cancellationToken))
            return ApplicationResult.NotFound("La imparticion no existe.");

        await profesoresDomain.EliminarImparticionAsync(profesorId, asignaturaId, cursoId, cancellationToken);
        return ApplicationResult.NoContent();
    }

    public async Task<ApplicationResult> PonerNotaAsync(int profesorId, PonerNotaDto dto, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();
        if (dto.Valor < 0 || dto.Valor > 10)
            return ApplicationResult.BadRequest("La nota debe estar entre 0 y 10.");

        var tareaInfo = await profesoresDomain.GetTareaInfoAsync(dto.TareaId, cancellationToken);
        if (tareaInfo is null)
            return ApplicationResult.NotFound("La tarea no existe.");
        if (tareaInfo.Value.ProfesorId != profesorId && !user.IsInRole("admin"))
            return ApplicationResult.Forbidden();

        var estudianteCursoId = await profesoresDomain.GetEstudianteCursoAsync(dto.EstudianteId, cancellationToken);
        if (estudianteCursoId is null)
            return ApplicationResult.NotFound("El estudiante no existe.");
        if (!await profesoresDomain.EstudianteMatriculadoAsync(dto.EstudianteId, tareaInfo.Value.AsignaturaId, cancellationToken))
            return ApplicationResult.BadRequest("El estudiante no esta matriculado en esa asignatura.");
        if (!await profesoresDomain.ProfesorImparteAlCursoAsync(tareaInfo.Value.ProfesorId, tareaInfo.Value.AsignaturaId, estudianteCursoId.Value, cancellationToken))
            return ApplicationResult.BadRequest("El profesor no imparte esa asignatura al curso del estudiante.");

        await profesoresDomain.SetNotaAsync(dto.EstudianteId, dto.TareaId, dto.Valor, cancellationToken);
        return ApplicationResult.Ok();
    }

    public async Task<ApplicationResult> CrearTareaAsync(int profesorId, CreateTareaDto dto, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return ApplicationResult.BadRequest("El nombre de la tarea es obligatorio.");
        if (dto.Trimestre < 1 || dto.Trimestre > 3)
            return ApplicationResult.BadRequest("El trimestre debe ser 1, 2 o 3.");
        if (!await profesoresDomain.ProfesorExisteAsync(profesorId, cancellationToken))
            return ApplicationResult.NotFound("El profesor no existe.");
        if (!await profesoresDomain.ProfesorImparteAsignaturaAsync(profesorId, dto.AsignaturaId, cancellationToken))
            return ApplicationResult.BadRequest("El profesor no imparte esa asignatura.");

        var asignaturaInfo = await profesoresDomain.GetAsignaturaInfoAsync(dto.AsignaturaId, cancellationToken);
        if (asignaturaInfo is null)
            return ApplicationResult.NotFound("La asignatura no existe.");

        var nombreNorm = dto.Nombre.Trim();
        if (await profesoresDomain.TareaDuplicadaAsync(dto.AsignaturaId, dto.Trimestre, nombreNorm, cancellationToken))
            return ApplicationResult.BadRequest($"Ya existe una tarea con el nombre '{nombreNorm}' en este trimestre para esta asignatura.");

        var tarea = await profesoresDomain.CrearTareaAsync(nombreNorm, dto.Trimestre, dto.AsignaturaId, profesorId, cancellationToken);
        return ApplicationResult.Created($"/api/profesores/{profesorId}/tareas/{tarea.Id}", tarea);
    }

    public async Task<ApplicationResult> GetTareasDeAsignaturaAsync(int profesorId, int asignaturaId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();

        var tareas = await profesoresDomain.GetTareasDeProfesorEnAsignaturaAsync(profesorId, asignaturaId, cancellationToken);
        return ApplicationResult.Ok(tareas);
    }

    public async Task<ApplicationResult> UpdateAsync(int id, UpdateProfesorDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return ApplicationResult.BadRequest("El nombre del profesor es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Correo))
            return ApplicationResult.BadRequest("El correo del profesor es obligatorio.");
        if (!await profesoresDomain.ProfesorExisteAsync(id, cancellationToken))
            return ApplicationResult.NotFound("El profesor no existe.");

        var correo = dto.Correo.Trim().ToLowerInvariant();
        if (await profesoresDomain.CorreoDuplicadoExceptAsync(correo, id, cancellationToken))
            return ApplicationResult.BadRequest("Ya existe otro profesor con ese correo.");

        string? hash = string.IsNullOrWhiteSpace(dto.NuevaContrasena)
            ? null
            : passwordService.Hash(dto.NuevaContrasena.Trim());

        var result = await profesoresDomain.UpdateAsync(id, dto.Nombre.Trim(), correo, hash, cancellationToken);
        return result is null
            ? ApplicationResult.NotFound("El profesor no existe.")
            : ApplicationResult.Ok(result);
    }

    public async Task<ApplicationResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!await profesoresDomain.ProfesorExisteAsync(id, cancellationToken))
            return ApplicationResult.NotFound("El profesor no existe.");

        await profesoresDomain.DeleteAsync(id, cancellationToken);
        return ApplicationResult.NoContent();
    }

    public async Task<ApplicationResult> GetTareasConNotasAsync(int asignaturaId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!user.IsInRole("admin"))
            return ApplicationResult.Forbidden();

        var asignatura = await profesoresDomain.GetAsignaturaInfoAsync(asignaturaId, cancellationToken);
        if (asignatura is null)
            return ApplicationResult.NotFound("La asignatura no existe.");

        var result = await profesoresDomain.GetTareasConNotasAsync(asignaturaId, cancellationToken);
        return ApplicationResult.Ok(result);
    }

    public async Task<ApplicationResult> GetStatsAsync(int profesorId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();

        var panel = await profesoresDomain.GetPanelAsync(profesorId, cancellationToken);
        if (panel is null)
            return ApplicationResult.NotFound("El profesor no existe.");

        var asignaturasStats = new List<AsignaturaStatsProfesorDto>();
        var mediasGlobales = new List<double>();

        foreach (var curso in panel.Cursos.OrderBy(c => c.Curso))
        {
            foreach (var asignatura in curso.Asignaturas.OrderBy(a => a.Nombre))
            {
                var alumnosBase = await profesoresDomain.GetAlumnosResumenAsync(asignatura.AsignaturaId, cancellationToken);
                var tareasProfesor = (await profesoresDomain.GetTareasDeProfesorEnAsignaturaAsync(profesorId, asignatura.AsignaturaId, cancellationToken)).ToList();

                var detallesAlumnos = new List<ProfesorAlumnoDetalleDto>();
                foreach (var alumno in alumnosBase)
                {
                    var detalle = await profesoresDomain.GetAlumnoDetalleAsync(asignatura.AsignaturaId, alumno.EstudianteId, cancellationToken);
                    if (detalle is not null)
                    {
                        detallesAlumnos.Add(detalle);
                    }
                }

                var finales = detallesAlumnos
                    .Where(d => d.NotaFinal.HasValue)
                    .Select(d => (double)d.NotaFinal!.Value)
                    .ToList();

                mediasGlobales.AddRange(finales);

                var porTarea = new List<TareaStatsDto>();
                foreach (var tarea in tareasProfesor)
                {
                    var calificaciones = await profesoresDomain.GetCalificacionesTareaAsync(asignatura.AsignaturaId, tarea.TareaId, cancellationToken);
                    var notas = calificaciones.Where(c => c.Valor.HasValue).Select(c => (double)c.Valor!.Value).ToList();

                    porTarea.Add(new TareaStatsDto
                    {
                        TareaId = tarea.TareaId,
                        Nombre = tarea.Nombre,
                        Trimestre = tarea.Trimestre,
                        Media = notas.Count > 0 ? Math.Round(notas.Average(), 2) : null,
                        TotalNotas = notas.Count,
                        SinNota = calificaciones.Count - notas.Count,
                        NotaMax = notas.Count > 0 ? NotaMaxima(notas) : null,
                        NotaMin = notas.Count > 0 ? NotaMinima(notas) : null
                    });
                }

                asignaturasStats.Add(new AsignaturaStatsProfesorDto
                {
                    AsignaturaId = asignatura.AsignaturaId,
                    Asignatura = asignatura.Nombre,
                    Curso = curso.Curso,
                    TotalAlumnos = alumnosBase.Count,
                    Aprobados = detallesAlumnos.Count(d => d.NotaFinal.HasValue && d.NotaFinal.Value >= 5),
                    Suspensos = detallesAlumnos.Count(d => d.NotaFinal.HasValue && d.NotaFinal.Value < 5),
                    SinNota = detallesAlumnos.Count(d => !d.NotaFinal.HasValue),
                    Media = finales.Count > 0 ? Math.Round(finales.Average(), 2) : null,
                    PorTarea = porTarea
                });
            }
        }

        return ApplicationResult.Ok(new ProfesorStatsDto
        {
            ProfesorId = panel.Id,
            Nombre = panel.Nombre,
            MediaGlobal = mediasGlobales.Count > 0 ? Math.Round(mediasGlobales.Average(), 2) : null,
            Asignaturas = asignaturasStats
        });
    }

    private static double NotaMaxima(IEnumerable<double> notas) => notas.Max();

    private static double NotaMinima(IEnumerable<double> notas) => notas.Min();

    private static bool UsuarioCoincideConProfesor(int profesorId, ClaimsPrincipal user)
    {
        if (user.IsInRole("admin")) return true;
        var idClaim = user.FindFirstValue("id") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idClaim, out var usuarioId) && usuarioId == profesorId;
    }
}
