using Back.Api.Application.Common;
using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Abstractions.Security;
using Back.Api.Application.Configuration;
using Back.Api.Application.Dtos;
using System.Security.Claims;

namespace Back.Api.Application.Services;

public class ProfesoresService(IProfesoresDomainRepository profesoresDomain, IPasswordService passwordService) : IProfesoresService
{
    public async Task<ApplicationResult> GetAllProfesoresAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await profesoresDomain.GetAllProfesoresAsync(cancellationToken));

    public async Task<ApplicationResult> GetSimpleProfesoresAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await profesoresDomain.GetSimpleProfesoresAsync(cancellationToken));

    public async Task<ApplicationResult> GetProfesorByIdAsync(int profesorId, CancellationToken cancellationToken = default)
    {
        var profesor = await profesoresDomain.GetDetalleAsync(profesorId, cancellationToken);
        return profesor is null
            ? ApplicationResult.NotFound("El profesor no existe.")
            : ApplicationResult.Ok(profesor);
    }

    public async Task<ApplicationResult> CreateProfesorAsync(CreateProfesorRequestDto createProfesorRequestDto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(createProfesorRequestDto.Nombre))
            return ApplicationResult.BadRequest("El nombre del profesor es obligatorio.");
        if (string.IsNullOrWhiteSpace(createProfesorRequestDto.Correo))
            return ApplicationResult.BadRequest("El correo del profesor es obligatorio.");
        if (string.IsNullOrWhiteSpace(createProfesorRequestDto.Contrasena))
            return ApplicationResult.BadRequest("La contrasena del profesor es obligatoria.");

        var correo = createProfesorRequestDto.Correo.Trim().ToLowerInvariant();
        if (await profesoresDomain.CorreoDuplicadoAsync(correo, cancellationToken))
            return ApplicationResult.BadRequest("Ya existe un profesor con ese correo.");

        var createdProfesor = await profesoresDomain.CreateProfesorAsync(createProfesorRequestDto.Nombre.Trim(), correo, passwordService.Hash(createProfesorRequestDto.Contrasena.Trim()), createProfesorRequestDto.Apellidos.Trim(), createProfesorRequestDto.DNI.Trim().ToUpperInvariant(), createProfesorRequestDto.Telefono.Trim(), createProfesorRequestDto.Especialidad.Trim(), cancellationToken);
        return ApplicationResult.Created($"/api/profesores/{createdProfesor.Id}", createdProfesor);
    }

    public async Task<ApplicationResult> GetPanelProfesorAsync(int profesorId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();

        var panel = await profesoresDomain.GetPanelAsync(profesorId, cancellationToken);
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

        var alumnosAsignatura = await profesoresDomain.GetAlumnosCompletoAsync(asignaturaId, cancellationToken);
        return alumnosAsignatura is null
            ? ApplicationResult.NotFound("La asignatura no existe.")
            : ApplicationResult.Ok(alumnosAsignatura);
    }

    public async Task<ApplicationResult> GetAlumnosResumenDeAsignaturaAsync(int profesorId, int asignaturaId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();
        if (!await profesoresDomain.ProfesorExisteAsync(profesorId, cancellationToken))
            return ApplicationResult.NotFound("El profesor no existe.");
        if (!await profesoresDomain.ProfesorImparteAsignaturaAsync(profesorId, asignaturaId, cancellationToken))
            return ApplicationResult.BadRequest("El profesor no imparte esta asignatura.");

        var alumnosResumen = await profesoresDomain.GetAlumnosResumenResponseAsync(asignaturaId, cancellationToken);
        return alumnosResumen is null
            ? ApplicationResult.NotFound("La asignatura no existe.")
            : ApplicationResult.Ok(alumnosResumen);
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

        var calificacionesTarea = await profesoresDomain.GetCalificacionesTareaResponseAsync(asignaturaId, tareaId, cancellationToken);
        return calificacionesTarea is null
            ? ApplicationResult.NotFound("La tarea no existe.")
            : ApplicationResult.Ok(calificacionesTarea);
    }

    public async Task<ApplicationResult> AsignarImparticionAsync(int profesorId, AsignarImparticionRequestDto asignarImparticionRequestDto, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();
        if (!await profesoresDomain.ProfesorExisteAsync(profesorId, cancellationToken))
            return ApplicationResult.NotFound("El profesor no existe.");

        var asignatura = await profesoresDomain.GetAsignaturaBasicaAsync(asignarImparticionRequestDto.AsignaturaId, cancellationToken);
        if (asignatura is null)
            return ApplicationResult.NotFound("La asignatura no existe.");
        if (!await profesoresDomain.CursoExisteAsync(asignarImparticionRequestDto.CursoId, cancellationToken))
            return ApplicationResult.NotFound("El curso no existe.");
        if (asignatura.Value.CursoId != asignarImparticionRequestDto.CursoId)
            return ApplicationResult.BadRequest("La asignatura no pertenece a ese curso.");
        if (await profesoresDomain.AsignaturaYaTieneOtroProfesorAsync(asignarImparticionRequestDto.AsignaturaId, profesorId, cancellationToken))
            return ApplicationResult.BadRequest("La asignatura ya tiene un profesor asignado.");
        if (await profesoresDomain.ImparticionExisteAsync(profesorId, asignarImparticionRequestDto.AsignaturaId, asignarImparticionRequestDto.CursoId, cancellationToken))
            return ApplicationResult.BadRequest("La imparticion ya existe para ese profesor, asignatura y curso.");

        await profesoresDomain.AsignarImparticionAsync(profesorId, asignarImparticionRequestDto.AsignaturaId, asignarImparticionRequestDto.CursoId, cancellationToken);
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

    public async Task<ApplicationResult> PonerNotaAsync(int profesorId, PonerNotaRequestDto ponerNotaRequestDto, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();
        if (ponerNotaRequestDto.Valor < 0 || ponerNotaRequestDto.Valor > 10)
            return ApplicationResult.BadRequest("La nota debe estar entre 0 y 10.");

        var tareaInfo = await profesoresDomain.GetTareaInfoAsync(ponerNotaRequestDto.TareaId, cancellationToken);
        if (tareaInfo is null)
            return ApplicationResult.NotFound("La tarea no existe.");
        if (tareaInfo.Value.ProfesorId != profesorId && !user.IsInRole(Roles.Admin))
            return ApplicationResult.Forbidden();

        var estudianteCursoId = await profesoresDomain.GetEstudianteCursoAsync(ponerNotaRequestDto.EstudianteId, cancellationToken);
        if (estudianteCursoId is null)
            return ApplicationResult.NotFound("El estudiante no existe.");
        if (!await profesoresDomain.EstudianteMatriculadoAsync(ponerNotaRequestDto.EstudianteId, tareaInfo.Value.AsignaturaId, cancellationToken))
            return ApplicationResult.BadRequest("El estudiante no esta matriculado en esa asignatura.");
        if (!await profesoresDomain.ProfesorImparteAlCursoAsync(tareaInfo.Value.ProfesorId, tareaInfo.Value.AsignaturaId, estudianteCursoId.Value, cancellationToken))
            return ApplicationResult.BadRequest("El profesor no imparte esa asignatura al curso del estudiante.");

        await profesoresDomain.SetNotaAsync(ponerNotaRequestDto.EstudianteId, ponerNotaRequestDto.TareaId, ponerNotaRequestDto.Valor, cancellationToken);
        return ApplicationResult.Ok();
    }

    public async Task<ApplicationResult> CrearTareaAsync(int profesorId, CreateTareaRequestDto createTareaRequestDto, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();
        if (string.IsNullOrWhiteSpace(createTareaRequestDto.Nombre))
            return ApplicationResult.BadRequest("El nombre de la tarea es obligatorio.");
        if (createTareaRequestDto.Trimestre < 1 || createTareaRequestDto.Trimestre > 3)
            return ApplicationResult.BadRequest("El trimestre debe ser 1, 2 o 3.");
        if (!await profesoresDomain.ProfesorExisteAsync(profesorId, cancellationToken))
            return ApplicationResult.NotFound("El profesor no existe.");
        if (!await profesoresDomain.ProfesorImparteAsignaturaAsync(profesorId, createTareaRequestDto.AsignaturaId, cancellationToken))
            return ApplicationResult.BadRequest("El profesor no imparte esa asignatura.");

        var asignaturaInfo = await profesoresDomain.GetAsignaturaInfoAsync(createTareaRequestDto.AsignaturaId, cancellationToken);
        if (asignaturaInfo is null)
            return ApplicationResult.NotFound("La asignatura no existe.");

        var nombreNorm = createTareaRequestDto.Nombre.Trim();
        if (await profesoresDomain.TareaDuplicadaAsync(createTareaRequestDto.AsignaturaId, createTareaRequestDto.Trimestre, nombreNorm, cancellationToken))
            return ApplicationResult.BadRequest($"Ya existe una tarea con el nombre '{nombreNorm}' en este trimestre para esta asignatura.");

        var tarea = await profesoresDomain.CrearTareaAsync(nombreNorm, createTareaRequestDto.Trimestre, createTareaRequestDto.AsignaturaId, profesorId, cancellationToken);
        return ApplicationResult.Created($"/api/profesores/{profesorId}/tareas/{tarea.Id}", tarea);
    }

    public async Task<ApplicationResult> GetTareasDeAsignaturaAsync(int profesorId, int asignaturaId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();

        var tareas = await profesoresDomain.GetTareasDeProfesorEnAsignaturaAsync(profesorId, asignaturaId, cancellationToken);
        return ApplicationResult.Ok(tareas);
    }

    public async Task<ApplicationResult> UpdateProfesorAsync(int profesorId, UpdateProfesorRequestDto updateProfesorRequestDto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(updateProfesorRequestDto.Nombre))
            return ApplicationResult.BadRequest("El nombre del profesor es obligatorio.");
        if (string.IsNullOrWhiteSpace(updateProfesorRequestDto.Correo))
            return ApplicationResult.BadRequest("El correo del profesor es obligatorio.");
        if (!await profesoresDomain.ProfesorExisteAsync(profesorId, cancellationToken))
            return ApplicationResult.NotFound("El profesor no existe.");

        var correo = updateProfesorRequestDto.Correo.Trim().ToLowerInvariant();
        if (await profesoresDomain.CorreoDuplicadoExceptAsync(correo, profesorId, cancellationToken))
            return ApplicationResult.BadRequest("Ya existe otro profesor con ese correo.");

        string? contrasenaHash = string.IsNullOrWhiteSpace(updateProfesorRequestDto.NuevaContrasena)
            ? null
            : passwordService.Hash(updateProfesorRequestDto.NuevaContrasena.Trim());

        var updatedProfesor = await profesoresDomain.UpdateProfesorAsync(profesorId, updateProfesorRequestDto.Nombre.Trim(), correo, contrasenaHash, updateProfesorRequestDto.Apellidos.Trim(), updateProfesorRequestDto.DNI.Trim().ToUpperInvariant(), updateProfesorRequestDto.Telefono.Trim(), updateProfesorRequestDto.Especialidad.Trim(), cancellationToken);
        return updatedProfesor is null
            ? ApplicationResult.NotFound("El profesor no existe.")
            : ApplicationResult.Ok(updatedProfesor);
    }

    public async Task<ApplicationResult> DeleteProfesorAsync(int profesorId, CancellationToken cancellationToken = default)
    {
        if (!await profesoresDomain.ProfesorExisteAsync(profesorId, cancellationToken))
            return ApplicationResult.NotFound("El profesor no existe.");

        await profesoresDomain.DeleteProfesorAsync(profesorId, cancellationToken);
        return ApplicationResult.NoContent();
    }

    public async Task<ApplicationResult> GetTareasConNotasAsync(int asignaturaId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!user.IsInRole(Roles.Admin))
            return ApplicationResult.Forbidden();

        var asignatura = await profesoresDomain.GetAsignaturaInfoAsync(asignaturaId, cancellationToken);
        if (asignatura is null)
            return ApplicationResult.NotFound("La asignatura no existe.");

        var tareasConNotas = await profesoresDomain.GetTareasConNotasAsync(asignaturaId, cancellationToken);
        return ApplicationResult.Ok(tareasConNotas);
    }

    public async Task<ApplicationResult> GetStatsAsync(int profesorId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();

        var profesorStats = await profesoresDomain.GetStatsAsync(profesorId, cancellationToken);
        if (profesorStats is null)
            return ApplicationResult.NotFound("El profesor no existe.");

        return ApplicationResult.Ok(profesorStats);
    }

    private static bool UsuarioCoincideConProfesor(int profesorId, ClaimsPrincipal user)
    {
        if (user.IsInRole(Roles.Admin)) return true;
        var idClaim = user.FindFirstValue("id") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idClaim, out var usuarioId) && usuarioId == profesorId;
    }
}
