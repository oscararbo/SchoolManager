using Back.Api.Application.Common;
using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Abstractions.Security;
using Back.Api.Application.Configuration;
using Back.Api.Application.Dtos;
using Back.Api.Application.Dtos.Profesores.Requests;
using Microsoft.AspNetCore.Hosting;
using System.Security.Claims;

namespace Back.Api.Application.Services;

public class ProfesoresService(IProfesoresDomainRepository profesoresDomain, IPasswordService passwordService, ICurrentSchoolContext currentSchoolContext, IWebHostEnvironment hostEnvironment) : IProfesoresService
{
    public async Task<ApplicationResult> GetAllProfesoresAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await profesoresDomain.GetAllProfesoresAsync(cancellationToken));

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
        if (string.IsNullOrWhiteSpace(createProfesorRequestDto.Nombre))
            return ApplicationResult.BadRequest("El nombre del teacher es obligatorio.");
        var normalizedDocumento = CredentialGenerationHelper.NormalizeDniNie(createProfesorRequestDto.DNI);
        if (!CredentialGenerationHelper.IsValidDniNie(normalizedDocumento))
            return ApplicationResult.BadRequest("El documento debe ser un DNI o NIE valido.");
        if (await profesoresDomain.DocumentoDuplicadoAsync(normalizedDocumento, cancellationToken))
            return ApplicationResult.BadRequest("Ya existe una persona con ese DNI/NIE.");

        var schoolSlug = CredentialGenerationHelper.NormalizeSchoolSlugForDomain(currentSchoolContext.SchoolSlug, currentSchoolContext.SchoolId);
        var generatedPassword = CredentialGenerationHelper.GeneratePassword();
        var generatedEmail = await GenerateUniqueEmailAsync($"{createProfesorRequestDto.Nombre} {createProfesorRequestDto.Apellidos}", "profesor", schoolSlug, cancellationToken);

        var createdProfesor = await profesoresDomain.CreateProfesorAsync(createProfesorRequestDto.Nombre.Trim(), generatedEmail, passwordService.Hash(generatedPassword), createProfesorRequestDto.Apellidos.Trim(), normalizedDocumento, createProfesorRequestDto.Telefono.Trim(), createProfesorRequestDto.Especialidad.Trim(), cancellationToken);
        createdProfesor.ContrasenaTemporal = generatedPassword;
        return ApplicationResult.Created($"/api/profesores/{createdProfesor.Id}", createdProfesor);
    }

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

    public async Task<ApplicationResult> SubirTareaSubmisionAsync(int profesorId, int tareaId, UploadTareaSubmisionRequestDto request, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();

        if (!await profesoresDomain.ProfesorImparteTareaAsync(profesorId, tareaId, cancellationToken))
            return ApplicationResult.BadRequest("El teacher no tiene acceso a esa task.");

        if (request.Archivo is null || request.Archivo.Length == 0)
            return ApplicationResult.BadRequest("Debes adjuntar un archivo.");

        if (request.Archivo.Length > 10 * 1024 * 1024)
            return ApplicationResult.BadRequest("El archivo supera el tamano maximo permitido (10MB).");

        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".txt", ".jpg", ".jpeg", ".png"
        };

        var extension = Path.GetExtension(request.Archivo.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
            return ApplicationResult.BadRequest("Tipo de archivo no permitido.");

        var tarea = await profesoresDomain.GetTareaInfoAsync(tareaId, cancellationToken);
        if (tarea is null)
            return ApplicationResult.NotFound("La task no existe.");

        if (!await profesoresDomain.EstudiantePerteneceAAsignaturaAsync(request.EstudianteId, tarea.Value.AsignaturaId, cancellationToken))
            return ApplicationResult.BadRequest("El estudiante no pertenece a la asignatura de la task.");

        var uploadsRoot = Path.Combine(hostEnvironment.ContentRootPath, "uploads", "tareas", tareaId.ToString());
        Directory.CreateDirectory(uploadsRoot);

        var safeBaseName = Path.GetFileNameWithoutExtension(request.Archivo.FileName);
        safeBaseName = string.Concat(safeBaseName.Where(ch => char.IsLetterOrDigit(ch) || ch == '-' || ch == '_'));
        if (string.IsNullOrWhiteSpace(safeBaseName)) safeBaseName = "archivo";

        var generatedFileName = $"{request.EstudianteId}_{DateTime.UtcNow:yyyyMMddHHmmss}_{safeBaseName}{extension}";
        var absolutePath = Path.Combine(uploadsRoot, generatedFileName);

        await using (var stream = File.Create(absolutePath))
        {
            await request.Archivo.CopyToAsync(stream, cancellationToken);
        }

        var relativePath = $"/uploads/tareas/{tareaId}/{generatedFileName}";
        var saved = await profesoresDomain.UpsertTareaSubmisionAsync(
            tareaId,
            request.EstudianteId,
            request.Archivo.FileName,
            relativePath,
            request.Archivo.Length,
            cancellationToken);

        return ApplicationResult.Ok(saved);
    }

    public async Task<ApplicationResult> GetSubmisionesDeTareaAsync(int profesorId, int tareaId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
            return ApplicationResult.Forbidden();

        if (!await profesoresDomain.ProfesorImparteTareaAsync(profesorId, tareaId, cancellationToken))
            return ApplicationResult.BadRequest("El teacher no tiene acceso a esa task.");

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

    private async Task<string> GenerateUniqueEmailAsync(string fullName, string rolePrefix, string schoolSlug, CancellationToken cancellationToken)
    {
        for (var i = 0; i < 2000; i++)
        {
            var candidate = CredentialGenerationHelper.BuildGeneratedEmail(fullName, rolePrefix, schoolSlug, i);
            if (!await profesoresDomain.CorreoDuplicadoAsync(candidate, cancellationToken))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("No se pudo generar un correo unico para el profesor.");
    }

    public async Task<ApplicationResult> DeleteProfesorAsync(int profesorId, CancellationToken cancellationToken = default)
    {
        if (!await profesoresDomain.ProfesorExisteAsync(profesorId, cancellationToken))
            return ApplicationResult.NotFound("El teacher no existe.");

        await profesoresDomain.DeleteProfesorAsync(profesorId, cancellationToken);
        return ApplicationResult.NoContent();
    }

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
}
