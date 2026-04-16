using Back.Api.Application.Common;
using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Abstractions.Security;
using Back.Api.Application.Configuration;
using Back.Api.Application.Dtos;
using System.Security.Claims;

namespace Back.Api.Application.Services;

public class ProfesoresService(IProfesoresDomainRepository profesoresDomain, IPasswordService passwordService) : IProfesoresService
{
    public async Task<ApplicationResult> GetAllAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await profesoresDomain.GetAllAsync(cancellationToken));

    public async Task<ApplicationResult> GetSimpleAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await profesoresDomain.GetSimpleAsync(cancellationToken));

    public async Task<ApplicationResult> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var profesor = await profesoresDomain.GetDetalleAsync(id, cancellationToken);
        return profesor is null
            ? ApplicationResult.NotFound("El profesor no existe.")
            : ApplicationResult.Ok(profesor);
    }

    public async Task<ApplicationResult> CreateAsync(CreateProfesorRequestDto dto, CancellationToken cancellationToken = default)
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

        var result = await profesoresDomain.CreateAsync(dto.Nombre.Trim(), correo, passwordService.Hash(dto.Contrasena.Trim()), dto.Apellidos.Trim(), dto.DNI.Trim(), dto.Telefono.Trim(), dto.Especialidad.Trim(), cancellationToken);
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

        var result = await profesoresDomain.GetAlumnosResumenResponseAsync(asignaturaId, cancellationToken);
        return result is null
            ? ApplicationResult.NotFound("La asignatura no existe.")
            : ApplicationResult.Ok(result);
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

        var result = await profesoresDomain.GetCalificacionesTareaResponseAsync(asignaturaId, tareaId, cancellationToken);
        return result is null
            ? ApplicationResult.NotFound("La tarea no existe.")
            : ApplicationResult.Ok(result);
    }

    public async Task<ApplicationResult> AsignarImparticionAsync(int profesorId, AsignarImparticionRequestDto dto, ClaimsPrincipal user, CancellationToken cancellationToken = default)
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

    public async Task<ApplicationResult> PonerNotaAsync(int profesorId, PonerNotaRequestDto dto, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();
        if (dto.Valor < 0 || dto.Valor > 10)
            return ApplicationResult.BadRequest("La nota debe estar entre 0 y 10.");

        var tareaInfo = await profesoresDomain.GetTareaInfoAsync(dto.TareaId, cancellationToken);
        if (tareaInfo is null)
            return ApplicationResult.NotFound("La tarea no existe.");
        if (tareaInfo.Value.ProfesorId != profesorId && !user.IsInRole(Roles.Admin))
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

    public async Task<ApplicationResult> CrearTareaAsync(int profesorId, CreateTareaRequestDto dto, ClaimsPrincipal user, CancellationToken cancellationToken = default)
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

    public async Task<ApplicationResult> UpdateAsync(int id, UpdateProfesorRequestDto dto, CancellationToken cancellationToken = default)
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

        var result = await profesoresDomain.UpdateAsync(id, dto.Nombre.Trim(), correo, hash, dto.Apellidos.Trim(), dto.DNI.Trim(), dto.Telefono.Trim(), dto.Especialidad.Trim(), cancellationToken);
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
        if (!user.IsInRole(Roles.Admin))
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

        var result = await profesoresDomain.GetStatsAsync(profesorId, cancellationToken);
        if (result is null)
            return ApplicationResult.NotFound("El profesor no existe.");

        return ApplicationResult.Ok(result);
    }

    private static bool UsuarioCoincideConProfesor(int profesorId, ClaimsPrincipal user)
    {
        if (user.IsInRole(Roles.Admin)) return true;
        var idClaim = user.FindFirstValue("id") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idClaim, out var usuarioId) && usuarioId == profesorId;
    }
}
