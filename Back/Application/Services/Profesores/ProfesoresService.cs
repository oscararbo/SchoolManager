using Back.Api.Application.Common;
using Back.Api.Application.Dtos;
using Back.Api.Domain.Repositories;
using Back.Api.Infrastructure.Security;
using System.Security.Claims;

namespace Back.Api.Application.Services;

public class ProfesoresService(IProfesoresDomainRepository profesoresDomain, IPasswordService passwordService) : IProfesoresService
{
    public async Task<ApplicationResult> GetAllAsync()
        => ApplicationResult.Ok(await profesoresDomain.GetAllAsync());

    public async Task<ApplicationResult> GetByIdAsync(int id)
    {
        var profesor = await profesoresDomain.GetDetalleAsync(id);
        return profesor is null
            ? ApplicationResult.NotFound("El profesor no existe.")
            : ApplicationResult.Ok(profesor);
    }

    public async Task<ApplicationResult> CreateAsync(CreateProfesorDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return ApplicationResult.BadRequest("El nombre del profesor es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Correo))
            return ApplicationResult.BadRequest("El correo del profesor es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Contrasena))
            return ApplicationResult.BadRequest("La contrasena del profesor es obligatoria.");

        var correo = dto.Correo.Trim().ToLowerInvariant();
        if (await profesoresDomain.CorreoDuplicadoAsync(correo))
            return ApplicationResult.BadRequest("Ya existe un profesor con ese correo.");

        var result = await profesoresDomain.CreateAsync(dto.Nombre.Trim(), correo, passwordService.Hash(dto.Contrasena.Trim()));
        return ApplicationResult.Created($"/api/profesores/{result.Id}", result);
    }

    public async Task<ApplicationResult> GetPanelProfesorAsync(int id, ClaimsPrincipal user)
    {
        if (!UsuarioCoincideConProfesor(id, user))
            return ApplicationResult.Forbidden();

        var panel = await profesoresDomain.GetPanelAsync(id);
        return panel is null
            ? ApplicationResult.NotFound("El profesor no existe.")
            : ApplicationResult.Ok(panel);
    }

    public async Task<ApplicationResult> GetAlumnosDeAsignaturaAsync(int profesorId, int asignaturaId, ClaimsPrincipal user)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();
        if (!await profesoresDomain.ProfesorExisteAsync(profesorId))
            return ApplicationResult.NotFound("El profesor no existe.");
        if (!await profesoresDomain.ProfesorImparteAsignaturaAsync(profesorId, asignaturaId))
            return ApplicationResult.BadRequest("El profesor no imparte esta asignatura.");

        var result = await profesoresDomain.GetAlumnosCompletoAsync(asignaturaId);
        return result is null
            ? ApplicationResult.NotFound("La asignatura no existe.")
            : ApplicationResult.Ok(result);
    }

    public async Task<ApplicationResult> GetAlumnosResumenDeAsignaturaAsync(int profesorId, int asignaturaId, ClaimsPrincipal user)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();
        if (!await profesoresDomain.ProfesorExisteAsync(profesorId))
            return ApplicationResult.NotFound("El profesor no existe.");
        if (!await profesoresDomain.ProfesorImparteAsignaturaAsync(profesorId, asignaturaId))
            return ApplicationResult.BadRequest("El profesor no imparte esta asignatura.");

        var asignaturaInfo = await profesoresDomain.GetAsignaturaInfoAsync(asignaturaId);
        if (asignaturaInfo is null)
            return ApplicationResult.NotFound("La asignatura no existe.");

        var tareas = await profesoresDomain.GetTareasDeAsignaturaAsync(asignaturaId);
        var alumnosBase = await profesoresDomain.GetAlumnosResumenAsync(asignaturaId);

        var alumnos = new List<AsignaturaAlumnoResumenDto>();
        foreach (var alumno in alumnosBase)
        {
            var detalle = await profesoresDomain.GetAlumnoDetalleAsync(asignaturaId, alumno.EstudianteId);
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

    public async Task<ApplicationResult> GetAlumnoDetalleDeAsignaturaAsync(int profesorId, int asignaturaId, int estudianteId, ClaimsPrincipal user)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();
        if (!await profesoresDomain.ProfesorExisteAsync(profesorId))
            return ApplicationResult.NotFound("El profesor no existe.");
        if (!await profesoresDomain.ProfesorImparteAsignaturaAsync(profesorId, asignaturaId))
            return ApplicationResult.BadRequest("El profesor no imparte esta asignatura.");

        var detalle = await profesoresDomain.GetAlumnoDetalleAsync(asignaturaId, estudianteId);
        return detalle is null
            ? ApplicationResult.NotFound("No se encontro el alumno en la asignatura indicada.")
            : ApplicationResult.Ok(detalle);
    }

    public async Task<ApplicationResult> GetCalificacionesDeTareaAsync(int profesorId, int asignaturaId, int tareaId, ClaimsPrincipal user)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();
        if (!await profesoresDomain.ProfesorExisteAsync(profesorId))
            return ApplicationResult.NotFound("El profesor no existe.");
        if (!await profesoresDomain.ProfesorImparteAsignaturaAsync(profesorId, asignaturaId))
            return ApplicationResult.BadRequest("El profesor no imparte esta asignatura.");
        if (!await profesoresDomain.ProfesorImparteTareaAsync(profesorId, tareaId))
            return ApplicationResult.BadRequest("El profesor no tiene acceso a esa tarea.");

        var tarea = await profesoresDomain.GetTareaResumenAsync(tareaId);
        if (tarea is null)
            return ApplicationResult.NotFound("La tarea no existe.");

        var calificaciones = await profesoresDomain.GetCalificacionesTareaAsync(asignaturaId, tareaId);
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

    public async Task<ApplicationResult> AsignarImparticionAsync(int profesorId, AsignarImparticionDto dto, ClaimsPrincipal user)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();
        if (!await profesoresDomain.ProfesorExisteAsync(profesorId))
            return ApplicationResult.NotFound("El profesor no existe.");

        var asignatura = await profesoresDomain.GetAsignaturaBasicaAsync(dto.AsignaturaId);
        if (asignatura is null)
            return ApplicationResult.NotFound("La asignatura no existe.");
        if (!await profesoresDomain.CursoExisteAsync(dto.CursoId))
            return ApplicationResult.NotFound("El curso no existe.");
        if (asignatura.Value.CursoId != dto.CursoId)
            return ApplicationResult.BadRequest("La asignatura no pertenece a ese curso.");
        if (await profesoresDomain.AsignaturaYaTieneOtroProfesorAsync(dto.AsignaturaId, profesorId))
            return ApplicationResult.BadRequest("La asignatura ya tiene un profesor asignado.");
        if (await profesoresDomain.ImparticionExisteAsync(profesorId, dto.AsignaturaId, dto.CursoId))
            return ApplicationResult.BadRequest("La imparticion ya existe para ese profesor, asignatura y curso.");

        await profesoresDomain.AsignarImparticionAsync(profesorId, dto.AsignaturaId, dto.CursoId);
        return ApplicationResult.Ok();
    }

    public async Task<ApplicationResult> PonerNotaAsync(int profesorId, PonerNotaDto dto, ClaimsPrincipal user)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();
        if (dto.Valor < 0 || dto.Valor > 10)
            return ApplicationResult.BadRequest("La nota debe estar entre 0 y 10.");

        var tareaInfo = await profesoresDomain.GetTareaInfoAsync(dto.TareaId);
        if (tareaInfo is null)
            return ApplicationResult.NotFound("La tarea no existe.");
        if (tareaInfo.Value.ProfesorId != profesorId && !user.IsInRole("admin"))
            return ApplicationResult.Forbidden();

        var estudianteCursoId = await profesoresDomain.GetEstudianteCursoAsync(dto.EstudianteId);
        if (estudianteCursoId is null)
            return ApplicationResult.NotFound("El estudiante no existe.");
        if (!await profesoresDomain.EstudianteMatriculadoAsync(dto.EstudianteId, tareaInfo.Value.AsignaturaId))
            return ApplicationResult.BadRequest("El estudiante no esta matriculado en esa asignatura.");
        if (!await profesoresDomain.ProfesorImparteAlCursoAsync(tareaInfo.Value.ProfesorId, tareaInfo.Value.AsignaturaId, estudianteCursoId.Value))
            return ApplicationResult.BadRequest("El profesor no imparte esa asignatura al curso del estudiante.");

        await profesoresDomain.SetNotaAsync(dto.EstudianteId, dto.TareaId, dto.Valor);
        return ApplicationResult.Ok();
    }

    public async Task<ApplicationResult> CrearTareaAsync(int profesorId, CreateTareaDto dto, ClaimsPrincipal user)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return ApplicationResult.BadRequest("El nombre de la tarea es obligatorio.");
        if (dto.Trimestre < 1 || dto.Trimestre > 3)
            return ApplicationResult.BadRequest("El trimestre debe ser 1, 2 o 3.");
        if (!await profesoresDomain.ProfesorExisteAsync(profesorId))
            return ApplicationResult.NotFound("El profesor no existe.");
        if (!await profesoresDomain.ProfesorImparteAsignaturaAsync(profesorId, dto.AsignaturaId))
            return ApplicationResult.BadRequest("El profesor no imparte esa asignatura.");

        var asignaturaInfo = await profesoresDomain.GetAsignaturaInfoAsync(dto.AsignaturaId);
        if (asignaturaInfo is null)
            return ApplicationResult.NotFound("La asignatura no existe.");

        var nombreNorm = dto.Nombre.Trim();
        if (await profesoresDomain.TareaDuplicadaAsync(dto.AsignaturaId, dto.Trimestre, nombreNorm))
            return ApplicationResult.BadRequest($"Ya existe una tarea con el nombre '{nombreNorm}' en este trimestre para esta asignatura.");

        var tarea = await profesoresDomain.CrearTareaAsync(nombreNorm, dto.Trimestre, dto.AsignaturaId, profesorId);
        return ApplicationResult.Created($"/api/profesores/{profesorId}/tareas/{tarea.Id}", tarea);
    }

    public async Task<ApplicationResult> GetTareasDeAsignaturaAsync(int profesorId, int asignaturaId, ClaimsPrincipal user)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();

        var tareas = await profesoresDomain.GetTareasDeProfesorEnAsignaturaAsync(profesorId, asignaturaId);
        return ApplicationResult.Ok(tareas);
    }

    public async Task<ApplicationResult> UpdateAsync(int id, UpdateProfesorDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return ApplicationResult.BadRequest("El nombre del profesor es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Correo))
            return ApplicationResult.BadRequest("El correo del profesor es obligatorio.");
        if (!await profesoresDomain.ProfesorExisteAsync(id))
            return ApplicationResult.NotFound("El profesor no existe.");

        var correo = dto.Correo.Trim().ToLowerInvariant();
        if (await profesoresDomain.CorreoDuplicadoExceptAsync(correo, id))
            return ApplicationResult.BadRequest("Ya existe otro profesor con ese correo.");

        string? hash = string.IsNullOrWhiteSpace(dto.NuevaContrasena)
            ? null
            : passwordService.Hash(dto.NuevaContrasena.Trim());

        var result = await profesoresDomain.UpdateAsync(id, dto.Nombre.Trim(), correo, hash);
        return result is null
            ? ApplicationResult.NotFound("El profesor no existe.")
            : ApplicationResult.Ok(result);
    }

    public async Task<ApplicationResult> DeleteAsync(int id)
    {
        if (!await profesoresDomain.ProfesorExisteAsync(id))
            return ApplicationResult.NotFound("El profesor no existe.");

        await profesoresDomain.DeleteAsync(id);
        return ApplicationResult.NoContent();
    }

    public async Task<ApplicationResult> GetTareasConNotasAsync(int asignaturaId, ClaimsPrincipal user)
    {
        if (!user.IsInRole("admin"))
            return ApplicationResult.Forbidden();

        var asignatura = await profesoresDomain.GetAsignaturaInfoAsync(asignaturaId);
        if (asignatura is null)
            return ApplicationResult.NotFound("La asignatura no existe.");

        var result = await profesoresDomain.GetTareasConNotasAsync(asignaturaId);
        return ApplicationResult.Ok(result);
    }

    private static bool UsuarioCoincideConProfesor(int profesorId, ClaimsPrincipal user)
    {
        if (user.IsInRole("admin")) return true;
        var idClaim = user.FindFirstValue("id") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idClaim, out var usuarioId) && usuarioId == profesorId;
    }
}
