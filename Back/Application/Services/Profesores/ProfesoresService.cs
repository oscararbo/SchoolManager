using Back.Api.Application.Common;
using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Abstractions.Security;
using Back.Api.Application.Configuration;
using Back.Api.Application.Dtos;
using Back.Api.Application.Dtos.Profesores.Requests;
using Back.Api.Application.Services.Common;
using System.Security.Claims;

namespace Back.Api.Application.Services;

public class ProfesoresService(IProfesoresDomainRepository profesoresDomain, IPasswordService passwordService, ICurrentSchoolContext currentSchoolContext, IWebHostEnvironment hostEnvironment, ICommonService commonService) : IProfesoresService
{
    #region CRUD profesores
    public async Task<ApplicationResult> GetAllProfesoresAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await profesoresDomain.GetAllProfesoresAsync(page, pageSize, cancellationToken));

    public async Task<ApplicationResult> GetSimpleProfesoresAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await profesoresDomain.GetSimpleProfesoresAsync(cancellationToken));

    public async Task<ApplicationResult> GetProfesorByIdAsync(int profesorId, CancellationToken cancellationToken = default)
    {
        var teacher = await profesoresDomain.GetDetalleAsync(profesorId, cancellationToken);
        return teacher is null
            ? ApplicationResult.NotFound("El teacher no existe.")
            : ApplicationResult.Ok(teacher);
    }

    public async Task<ApplicationResult> CreateProfesorAsync(CreateProfesorRequestDto createProfesorRequestDto, CancellationToken cancellationToken = default)
    
{
        var required = commonService.Required(createProfesorRequestDto.Nombre, "Nombre obligatorio.");
        if (required != null) return required;

        var dni = CredentialGenerationHelper.NormalizeDniNie(createProfesorRequestDto.DNI);

        if (!CredentialGenerationHelper.IsValidDniNie(dni))
            return ApplicationResult.BadRequest("DNI inválido.");

        var slug = CredentialGenerationHelper.NormalizeSchoolSlugForDomain(currentSchoolContext.SchoolSlug, currentSchoolContext.SchoolId);

        var pass = CredentialGenerationHelper.GeneratePassword();

        var email = await commonService.GenerateUniqueEmailAsync(
            $"{createProfesorRequestDto.Nombre} {createProfesorRequestDto.Apellidos}",
            "profesor",
            slug,
            e => profesoresDomain.CorreoDuplicadoAsync(e, cancellationToken));

        var profesor = await profesoresDomain.CreateProfesorAsync(
            commonService.Normalize(createProfesorRequestDto.Nombre),
            email,
            passwordService.Hash(pass),
            commonService.Normalize(createProfesorRequestDto.Apellidos),
            dni,
            createProfesorRequestDto.Telefono.Trim(),
            createProfesorRequestDto.Especialidad.Trim(),
            cancellationToken);

        profesor.ContrasenaTemporal = pass;

        return ApplicationResult.Created($"/api/profesores/{profesor.Id}", profesor);
    }

    public async Task<ApplicationResult> UpdateProfesorAsync(int profesorId, UpdateProfesorRequestDto updateProfesorRequestDto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(updateProfesorRequestDto.Nombre))
            return ApplicationResult.BadRequest("El nombre del teacher es obligatorio.");
        if (!await profesoresDomain.ProfesorExisteAsync(profesorId, cancellationToken))
            return ApplicationResult.NotFound("El teacher no existe.");

        var normalizedDocumento = CredentialGenerationHelper.NormalizeDniNie(updateProfesorRequestDto.DNI);
        if (!CredentialGenerationHelper.IsValidDniNie(normalizedDocumento))
            return ApplicationResult.BadRequest("El documento debe ser un DNI o NIE valido.");
        if (await profesoresDomain.DocumentoDuplicadoExceptAsync(normalizedDocumento, profesorId, cancellationToken))
            return ApplicationResult.BadRequest("Ya existe una persona con ese DNI/NIE.");

        var updatedProfesor = await profesoresDomain.UpdateProfesorAsync(profesorId, updateProfesorRequestDto.Nombre.Trim(), updateProfesorRequestDto.Apellidos.Trim(), normalizedDocumento, updateProfesorRequestDto.Telefono.Trim(), updateProfesorRequestDto.Especialidad.Trim(), cancellationToken);
        return updatedProfesor is null
            ? ApplicationResult.NotFound("El teacher no existe.")
            : ApplicationResult.Ok(updatedProfesor);
    }

    public async Task<ApplicationResult> DeleteProfesorAsync(int profesorId, CancellationToken cancellationToken = default)
    {
        if (!await profesoresDomain.ProfesorExisteAsync(profesorId, cancellationToken))
            return ApplicationResult.NotFound("El teacher no existe.");

        await profesoresDomain.DeleteProfesorAsync(profesorId, cancellationToken);
        return ApplicationResult.NoContent();
    }
    #endregion

    #region Panel y alumnos
    public async Task<ApplicationResult> GetPanelProfesorAsync(int profesorId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();

        var panel = await profesoresDomain.GetPanelAsync(profesorId, cancellationToken);
        return panel is null
            ? ApplicationResult.NotFound("El teacher no existe.")
            : ApplicationResult.Ok(panel);
    }

    public async Task<ApplicationResult> GetAlumnosDeAsignaturaAsync(int profesorId, int asignaturaId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        var guard = await ValidarProfesorYAsignaturaAsync(profesorId, asignaturaId, user, cancellationToken);
        if (guard is not null) return guard;

        var alumnosAsignatura = await profesoresDomain.GetAlumnosCompletoAsync(asignaturaId, cancellationToken);
        return alumnosAsignatura is null
            ? ApplicationResult.NotFound("La subject no existe.")
            : ApplicationResult.Ok(alumnosAsignatura);
    }

    public async Task<ApplicationResult> GetAlumnosResumenDeAsignaturaAsync(int profesorId, int asignaturaId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        var guard = await ValidarProfesorYAsignaturaAsync(profesorId, asignaturaId, user, cancellationToken);
        if (guard is not null) return guard;

        var alumnosResumen = await profesoresDomain.GetAlumnosResumenResponseAsync(asignaturaId, cancellationToken);
        return alumnosResumen is null
            ? ApplicationResult.NotFound("La subject no existe.")
            : ApplicationResult.Ok(alumnosResumen);
    }

    public async Task<ApplicationResult> GetAlumnoDetalleDeAsignaturaAsync(int profesorId, int asignaturaId, int estudianteId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        var guard = await ValidarProfesorYAsignaturaAsync(profesorId, asignaturaId, user, cancellationToken);
        if (guard is not null) return guard;

        var detail = await profesoresDomain.GetAlumnoDetalleAsync(asignaturaId, estudianteId, cancellationToken);
        return detail is null
            ? ApplicationResult.NotFound("No se encontro el alumno en la subject indicada.")
            : ApplicationResult.Ok(detail);
    }
    #endregion

    #region Calificaciones y tareas
    public async Task<ApplicationResult> GetCalificacionesDeTareaAsync(int profesorId, int asignaturaId, int tareaId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        var guard = await ValidarProfesorYAsignaturaAsync(profesorId, asignaturaId, user, cancellationToken);
        if (guard is not null) return guard;

        if (!await profesoresDomain.ProfesorImparteTareaAsync(profesorId, tareaId, cancellationToken))
            return ApplicationResult.BadRequest("El teacher no tiene acceso a esa task.");

        var calificacionesTarea = await profesoresDomain.GetCalificacionesTareaResponseAsync(asignaturaId, tareaId, cancellationToken);
        return calificacionesTarea is null
            ? ApplicationResult.NotFound("La task no existe.")
            : ApplicationResult.Ok(calificacionesTarea);
    }

    public async Task<ApplicationResult> AsignarImparticionAsync(int profesorId, AsignarImparticionRequestDto asignarImparticionRequestDto, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();
        if (!await profesoresDomain.ProfesorExisteAsync(profesorId, cancellationToken))
            return ApplicationResult.NotFound("El teacher no existe.");

        var subject = await profesoresDomain.GetAsignaturaBasicaAsync(asignarImparticionRequestDto.AsignaturaId, cancellationToken);
        if (subject is null)
            return ApplicationResult.NotFound("La subject no existe.");
        if (!await profesoresDomain.CursoExisteAsync(asignarImparticionRequestDto.CursoId, cancellationToken))
            return ApplicationResult.NotFound("El curso no existe.");
        if (subject.Value.CursoId != asignarImparticionRequestDto.CursoId)
            return ApplicationResult.BadRequest("La subject no pertenece a ese curso.");
        if (await profesoresDomain.AsignaturaYaTieneOtroProfesorAsync(asignarImparticionRequestDto.AsignaturaId, profesorId, cancellationToken))
            return ApplicationResult.BadRequest("La subject ya tiene un teacher asignado.");
        if (await profesoresDomain.ImparticionExisteAsync(profesorId, asignarImparticionRequestDto.AsignaturaId, asignarImparticionRequestDto.CursoId, cancellationToken))
            return ApplicationResult.BadRequest("La imparticion ya existe para ese teacher, subject y curso.");

        await profesoresDomain.AsignarImparticionAsync(profesorId, asignarImparticionRequestDto.AsignaturaId, asignarImparticionRequestDto.CursoId, cancellationToken);
        return ApplicationResult.Ok();
    }

    public async Task<ApplicationResult> EliminarImparticionAsync(int profesorId, int asignaturaId, int cursoId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();
        if (!await profesoresDomain.ProfesorExisteAsync(profesorId, cancellationToken))
            return ApplicationResult.NotFound("El teacher no existe.");
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

        var taskInfo = await profesoresDomain.GetTareaInfoAsync(ponerNotaRequestDto.TareaId, cancellationToken);
        if (taskInfo is null)
            return ApplicationResult.NotFound("La task no existe.");
        if (taskInfo.Value.ProfesorId != profesorId && !user.IsInRole(Roles.Admin))
            return ApplicationResult.Forbidden();

        var studentCourseId = await profesoresDomain.GetEstudianteCursoAsync(ponerNotaRequestDto.EstudianteId, cancellationToken);
        if (studentCourseId is null)
            return ApplicationResult.NotFound("El estudiante no existe.");
        if (!await profesoresDomain.EstudianteMatriculadoAsync(ponerNotaRequestDto.EstudianteId, taskInfo.Value.AsignaturaId, cancellationToken))
            return ApplicationResult.BadRequest("El estudiante no esta matriculado en esa subject.");
        if (!await profesoresDomain.ProfesorImparteAlCursoAsync(taskInfo.Value.ProfesorId, taskInfo.Value.AsignaturaId, studentCourseId.Value, cancellationToken))
            return ApplicationResult.BadRequest("El teacher no imparte esa subject al curso del estudiante.");

        await profesoresDomain.SetNotaAsync(ponerNotaRequestDto.EstudianteId, ponerNotaRequestDto.TareaId, ponerNotaRequestDto.Valor, cancellationToken);
        return ApplicationResult.Ok();
    }

    public async Task<ApplicationResult> CrearTareaAsync(int profesorId, CreateTareaRequestDto createTareaRequestDto, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();
        if (string.IsNullOrWhiteSpace(createTareaRequestDto.Nombre))
            return ApplicationResult.BadRequest("El nombre de la task es obligatorio.");
        if (createTareaRequestDto.Trimestre < 1 || createTareaRequestDto.Trimestre > 3)
            return ApplicationResult.BadRequest("El trimestre debe ser 1, 2 o 3.");
        if (!await profesoresDomain.ProfesorExisteAsync(profesorId, cancellationToken))
            return ApplicationResult.NotFound("El teacher no existe.");
        if (!await profesoresDomain.ProfesorImparteAsignaturaAsync(profesorId, createTareaRequestDto.AsignaturaId, cancellationToken))
            return ApplicationResult.BadRequest("El teacher no imparte esa subject.");

        var asignaturaInfo = await profesoresDomain.GetAsignaturaInfoAsync(createTareaRequestDto.AsignaturaId, cancellationToken);
        if (asignaturaInfo is null)
            return ApplicationResult.NotFound("La subject no existe.");

        var normalizedName = createTareaRequestDto.Nombre.Trim();
        if (await profesoresDomain.TareaDuplicadaAsync(createTareaRequestDto.AsignaturaId, createTareaRequestDto.Trimestre, normalizedName, cancellationToken))
            return ApplicationResult.BadRequest($"Ya existe una task con el nombre '{normalizedName}' en este trimestre para esta subject.");

        var normalizedDescripcion = string.IsNullOrWhiteSpace(createTareaRequestDto.Descripcion)
            ? null
            : createTareaRequestDto.Descripcion.Trim();
        var task = await profesoresDomain.CrearTareaAsync(normalizedName, normalizedDescripcion, createTareaRequestDto.Trimestre, createTareaRequestDto.AsignaturaId, profesorId, cancellationToken);
        return ApplicationResult.Created($"/api/profesores/{profesorId}/tasks/{task.Id}", task);
    }

    public async Task<ApplicationResult> UpdateTareaDescripcionAsync(int profesorId, int tareaId, UpdateTareaDescripcionRequestDto request, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();

        if (!await profesoresDomain.ProfesorImparteTareaAsync(profesorId, tareaId, cancellationToken))
            return ApplicationResult.BadRequest("El teacher no tiene acceso a esa task.");

        var descripcion = string.IsNullOrWhiteSpace(request.Descripcion) ? null : request.Descripcion.Trim();
        var updated = await profesoresDomain.UpdateTareaDescripcionAsync(tareaId, descripcion, cancellationToken);
        return updated is null
            ? ApplicationResult.NotFound("La task no existe.")
            : ApplicationResult.Ok(updated);
    }

public async Task<ApplicationResult> SubirTareaSubmisionAsync(int profesorId, int tareaId, UploadTareaSubmisionRequestDto uploadTareaSubmisionRequestDto, ClaimsPrincipal user, CancellationToken cancellationToken = default)
{
    if (!commonService.UsuarioCoincide(profesorId, user))
        return ApplicationResult.Forbidden();

    if (!await profesoresDomain.ProfesorImparteTareaAsync(profesorId, tareaId, cancellationToken))
        return ApplicationResult.BadRequest("El profesor no tiene acceso a esa tarea.");
        
    var fileValidation = commonService.ValidateFile(uploadTareaSubmisionRequestDto.Archivo);
    if (fileValidation != null)
        return fileValidation;

    var file = uploadTareaSubmisionRequestDto.Archivo!;

    var tarea = await profesoresDomain.GetTareaInfoAsync(tareaId, cancellationToken);
    if (tarea is null)
        return ApplicationResult.NotFound("La tarea no existe.");

    if (!await profesoresDomain.EstudiantePerteneceAAsignaturaAsync(
            uploadTareaSubmisionRequestDto.EstudianteId,
            tarea.Value.AsignaturaId,
            cancellationToken))
    {
        return ApplicationResult.BadRequest("El estudiante no pertenece a la asignatura.");
    }

    var path = await commonService.SaveFileAsync(
        file.OpenReadStream(),
        file.FileName,
        hostEnvironment.ContentRootPath,
        $"uploads/tareas/{tareaId}",
        cancellationToken);

    var saved = await profesoresDomain.UpsertTareaSubmisionAsync(
        tareaId,
        uploadTareaSubmisionRequestDto.EstudianteId,
        file.FileName,
        path,
        file.Length,
        cancellationToken);

    return ApplicationResult.Ok(saved);
}

    public async Task<ApplicationResult> GetSubmisionesDeTareaAsync(int profesorId, int tareaId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();

        if (!await profesoresDomain.ProfesorImparteTareaAsync(profesorId, tareaId, cancellationToken))
            return ApplicationResult.BadRequest("El profesor no tiene acceso a esa tarea.");

        var submisiones = await profesoresDomain.GetSubmisionesDeTareaAsync(tareaId, cancellationToken);
        return ApplicationResult.Ok(submisiones);
    }

    public async Task<ApplicationResult> DeleteSubmisionAsync(int profesorId, int submisionId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();

        var removed = await profesoresDomain.DeleteSubmisionAsync(submisionId, cancellationToken);
        return removed
            ? ApplicationResult.NoContent()
            : ApplicationResult.NotFound("La submision no existe.");
    }

    public async Task<ApplicationResult> GetTareasDeAsignaturaAsync(int profesorId, int asignaturaId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();

        var tasks = await profesoresDomain.GetTareasDeProfesorEnAsignaturaAsync(profesorId, asignaturaId, cancellationToken);
        return ApplicationResult.Ok(tasks);
    }
    #endregion

    #region Estadisticas y validaciones
    public async Task<ApplicationResult> GetTareasConNotasAsync(int asignaturaId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!user.IsInRole(Roles.Admin))
            return ApplicationResult.Forbidden();

        var subject = await profesoresDomain.GetAsignaturaInfoAsync(asignaturaId, cancellationToken);
        if (subject is null)
            return ApplicationResult.NotFound("La subject no existe.");

        var tareasConNotas = await profesoresDomain.GetTareasConNotasAsync(asignaturaId, cancellationToken);
        return ApplicationResult.Ok(tareasConNotas);
    }

    public async Task<ApplicationResult> GetStatsAsync(int profesorId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();

        var profesorStats = await profesoresDomain.GetStatsAsync(profesorId, cancellationToken);
        if (profesorStats is null)
            return ApplicationResult.NotFound("El teacher no existe.");

        return ApplicationResult.Ok(profesorStats);
    }

    private async Task<ApplicationResult?> ValidarProfesorYAsignaturaAsync(int profesorId, int asignaturaId, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();
        if (!await profesoresDomain.ProfesorExisteAsync(profesorId, cancellationToken))
            return ApplicationResult.NotFound("El teacher no existe.");
        if (!await profesoresDomain.ProfesorImparteAsignaturaAsync(profesorId, asignaturaId, cancellationToken))
            return ApplicationResult.BadRequest("El teacher no imparte esta subject.");
        return null;
    }

    private static bool UsuarioCoincideConProfesor(int profesorId, ClaimsPrincipal user)
    {
        if (user.IsInRole(Roles.Admin)) return true;
        var idClaim = user.FindFirstValue("id") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idClaim, out var usuarioId) && usuarioId == profesorId;
    }
    #endregion
}
