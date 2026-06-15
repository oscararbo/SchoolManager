using Back.Api.Application.Common;
using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Abstractions.Security;
using Back.Api.Application.Configuration;
using Back.Api.Application.Dtos;
using System.Security.Claims;
using Back.Api.Application.Services.Common;

namespace Back.Api.Application.Services;

public class EstudiantesService(IEstudiantesDomainRepository estudiantesDomain, IPasswordService passwordService, ICurrentSchoolContext currentSchoolContext, IWebHostEnvironment hostEnvironment, ICommonService commonService) : IEstudiantesService
{
    #region CRUD estudiantes
    public async Task<ApplicationResult> GetAllEstudiantesAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await estudiantesDomain.GetAllEstudiantesAsync(page, pageSize, cancellationToken));

    public async Task<ApplicationResult> GetSimpleEstudiantesAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await estudiantesDomain.GetSimpleEstudiantesAsync(cancellationToken));

    public async Task<ApplicationResult> GetEstudianteByIdAsync(int estudianteId, CancellationToken cancellationToken = default)
    {
        var estudiante = await estudiantesDomain.GetDetalleAsync(estudianteId, cancellationToken);
        return estudiante is null ? ApplicationResult.NotFound("El estudiante no existe.") : ApplicationResult.Ok(estudiante);
    }

    public async Task<ApplicationResult> CreateEstudianteAsync(CreateEstudianteRequestDto createEstudianteRequestDto, CancellationToken cancellationToken = default)
    
{
        var required = commonService.Required(createEstudianteRequestDto.Nombre, "El nombre del estudiante es obligatorio.");
        if (required != null) return required;

        if (createEstudianteRequestDto.CursoId <= 0)
            return ApplicationResult.BadRequest("El curso es obligatorio.");

        var dni = CredentialGenerationHelper.NormalizeDniNie(createEstudianteRequestDto.DNI);

        if (!CredentialGenerationHelper.IsValidDniNie(dni))
            return ApplicationResult.BadRequest("El documento debe ser un DNI o NIE valido.");

        if (await estudiantesDomain.DocumentoDuplicadoAsync(dni, cancellationToken))
            return ApplicationResult.BadRequest("Ya existe una persona con ese DNI/NIE.");

        var schoolSlug = CredentialGenerationHelper.NormalizeSchoolSlugForDomain(
            currentSchoolContext.SchoolSlug,
            currentSchoolContext.SchoolId);

        var password = CredentialGenerationHelper.GeneratePassword();

        var email = await commonService.GenerateUniqueEmailAsync(
            $"{createEstudianteRequestDto.Nombre} {createEstudianteRequestDto.Apellidos}",
            "alumno",
            schoolSlug,
            e => estudiantesDomain.CorreoDuplicadoAsync(e, cancellationToken));

        var estudiante = await estudiantesDomain.CreateEstudianteAsync(
            commonService.Normalize(createEstudianteRequestDto.Nombre),
            email,
            createEstudianteRequestDto.CursoId,
            passwordService.Hash(password),
            commonService.Normalize(createEstudianteRequestDto.Apellidos),
            dni,
            createEstudianteRequestDto.Telefono.Trim(),
            createEstudianteRequestDto.FechaNacimiento!.Value,
            cancellationToken);

        estudiante.ContrasenaTemporal = password;

        return ApplicationResult.Created($"/api/estudiantes/{estudiante.Id}", estudiante);
    }

    public async Task<ApplicationResult> MatricularAsync(int estudianteId, int asignaturaId, CancellationToken cancellationToken = default)
    {
        if (!await estudiantesDomain.AsignaturaExisteAsync(asignaturaId, cancellationToken))
            return ApplicationResult.NotFound("La asignatura no existe.");

        var studentDetail = await estudiantesDomain.GetDetalleAsync(estudianteId, cancellationToken);
        if (studentDetail is null)
            return ApplicationResult.NotFound("El estudiante no existe.");

        if (!await estudiantesDomain.AsignaturaEsDelCursoAsync(asignaturaId, studentDetail.CursoId, cancellationToken))
            return ApplicationResult.BadRequest("El estudiante solo puede matricularse en asignaturas de su curso.");

        if (await estudiantesDomain.YaMatriculadoAsync(estudianteId, asignaturaId, cancellationToken))
            return ApplicationResult.BadRequest("El estudiante ya esta matriculado en esta asignatura.");

        await estudiantesDomain.MatricularAsync(estudianteId, asignaturaId, cancellationToken);
        return ApplicationResult.Ok();
    }

    public async Task<ApplicationResult> DesmatricularAsync(int estudianteId, int asignaturaId, CancellationToken cancellationToken = default)
    {
        if (!await estudiantesDomain.ExisteAsync(estudianteId, cancellationToken))
            return ApplicationResult.NotFound("El estudiante no existe.");
        if (!await estudiantesDomain.AsignaturaExisteAsync(asignaturaId, cancellationToken))
            return ApplicationResult.NotFound("La asignatura no existe.");
        if (!await estudiantesDomain.YaMatriculadoAsync(estudianteId, asignaturaId, cancellationToken))
            return ApplicationResult.BadRequest("El estudiante no esta matriculado en esa asignatura.");

        await estudiantesDomain.DesmatricularAsync(estudianteId, asignaturaId, cancellationToken);
        return ApplicationResult.NoContent();
    }
    #endregion

    #region Panel y materias
    public async Task<ApplicationResult> GetPanelAlumnoAsync(int estudianteId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConEstudiante(estudianteId, user))
            return ApplicationResult.Forbidden();

        var panel = await estudiantesDomain.GetPanelAlumnoAsync(estudianteId, cancellationToken);
        return panel is null
            ? ApplicationResult.NotFound("El estudiante no existe.")
            : ApplicationResult.Ok(panel);
    }

    public async Task<ApplicationResult> GetPanelResumenAsync(int estudianteId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConEstudiante(estudianteId, user))
            return ApplicationResult.Forbidden();

        var panel = await estudiantesDomain.GetPanelResumenAsync(estudianteId, cancellationToken);
        return panel is null
            ? ApplicationResult.NotFound("El estudiante no existe.")
            : ApplicationResult.Ok(panel);
    }

    public async Task<ApplicationResult> GetHorarioAlumnoAsync(int estudianteId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConEstudiante(estudianteId, user))
            return ApplicationResult.Forbidden();

        var horario = await estudiantesDomain.GetHorarioAlumnoAsync(estudianteId, cancellationToken);
        return horario is null
            ? ApplicationResult.NotFound("El estudiante no existe.")
            : ApplicationResult.Ok(horario);
    }

    public async Task<ApplicationResult> GetMateriaDetalleAsync(int estudianteId, int asignaturaId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConEstudiante(estudianteId, user))
            return ApplicationResult.Forbidden();

        var detail = await estudiantesDomain.GetMateriaDetalleAsync(estudianteId, asignaturaId, cancellationToken);
        return detail is null
            ? ApplicationResult.NotFound("La asignatura o el estudiante no existe.")
            : ApplicationResult.Ok(detail);
    }
    #endregion

    #region Actualizacion y helpers de identidad
    public async Task<ApplicationResult> UpdateEstudianteAsync(int estudianteId, UpdateEstudianteRequestDto updateEstudianteRequestDto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(updateEstudianteRequestDto.Nombre))
            return ApplicationResult.BadRequest("El nombre del estudiante es obligatorio.");
        if (updateEstudianteRequestDto.CursoId <= 0)
            return ApplicationResult.BadRequest("El curso del estudiante es obligatorio.");
        if (!await estudiantesDomain.ExisteAsync(estudianteId, cancellationToken))
            return ApplicationResult.NotFound("El estudiante no existe.");

        var normalizedDocumento = CredentialGenerationHelper.NormalizeDniNie(updateEstudianteRequestDto.DNI);
        if (!CredentialGenerationHelper.IsValidDniNie(normalizedDocumento))
            return ApplicationResult.BadRequest("El documento debe ser un DNI o NIE valido.");
        if (await estudiantesDomain.DocumentoDuplicadoExceptAsync(normalizedDocumento, estudianteId, cancellationToken))
            return ApplicationResult.BadRequest("Ya existe una persona con ese DNI/NIE.");
        if (!await estudiantesDomain.CursoExisteAsync(updateEstudianteRequestDto.CursoId, cancellationToken))
            return ApplicationResult.BadRequest("El curso indicado no existe.");

        var updatedEstudiante = await estudiantesDomain.UpdateEstudianteAsync(estudianteId, updateEstudianteRequestDto.Nombre.Trim(), updateEstudianteRequestDto.CursoId, updateEstudianteRequestDto.Apellidos.Trim(), normalizedDocumento, updateEstudianteRequestDto.Telefono.Trim(), updateEstudianteRequestDto.FechaNacimiento!.Value, cancellationToken);
        return updatedEstudiante is null
            ? ApplicationResult.NotFound("El estudiante no existe.")
            : ApplicationResult.Ok(updatedEstudiante);
    }

    public async Task<ApplicationResult> DeleteEstudianteAsync(int estudianteId, CancellationToken cancellationToken = default)
    {
        if (!await estudiantesDomain.ExisteAsync(estudianteId, cancellationToken))
            return ApplicationResult.NotFound("El estudiante no existe.");

        await estudiantesDomain.DeleteEstudianteAsync(estudianteId, cancellationToken);
        return ApplicationResult.NoContent();
    }

    private static bool UsuarioCoincideConEstudiante(int estudianteId, ClaimsPrincipal user)
    {
        if (user.IsInRole(Roles.Admin)) return true;
        var idClaim = user.FindFirstValue("id") ?? user.FindFirstValue(ClaimsIdentity.DefaultNameClaimType);
        return int.TryParse(idClaim, out var usuarioId) && usuarioId == estudianteId;
    }
    #endregion

    #region Submisiones y tareas
    public async Task<ApplicationResult> SubirSubmisionAsync(int estudianteId, int tareaId, IFormFile archivo, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConEstudiante(estudianteId, user))
            return ApplicationResult.Forbidden();

        var fileValidation = commonService.ValidateFile(archivo);
        if (fileValidation != null)
            return fileValidation;

        if (!await estudiantesDomain.EstudianteMatriculadoEnTareaAsync(estudianteId, tareaId, cancellationToken))
            return ApplicationResult.BadRequest("El estudiante no tiene acceso a esa tarea.");

        var path = await commonService.SaveFileAsync(
            archivo.OpenReadStream(),
            archivo.FileName,
            hostEnvironment.ContentRootPath,
            $"uploads/tareas/{tareaId}",
            cancellationToken);

        var saved = await estudiantesDomain.UpsertSubmisionEstudianteAsync(estudianteId, tareaId, archivo.FileName, path, archivo.Length, cancellationToken);
        return ApplicationResult.Ok(saved);
    }

    public async Task<ApplicationResult> GetSubmisionesAsync(int estudianteId, int tareaId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConEstudiante(estudianteId, user))
            return ApplicationResult.Forbidden();

        var submisiones = await estudiantesDomain.GetSubmisionesEstudianteAsync(estudianteId, tareaId, cancellationToken);
        return ApplicationResult.Ok(submisiones);
    }

    public async Task<ApplicationResult> DeleteSubmisionAsync(int estudianteId, int submisionId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConEstudiante(estudianteId, user))
            return ApplicationResult.Forbidden();

        var removed = await estudiantesDomain.DeleteSubmisionEstudianteAsync(estudianteId, submisionId, cancellationToken);
        return removed ? ApplicationResult.NoContent() : ApplicationResult.NotFound("La submision no existe.");
    }

    public async Task<ApplicationResult> MarcarTareaHechaAsync(int estudianteId, int tareaId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConEstudiante(estudianteId, user))
            return ApplicationResult.Forbidden();

        if (!await estudiantesDomain.EstudianteMatriculadoEnTareaAsync(estudianteId, tareaId, cancellationToken))
            return ApplicationResult.BadRequest("El estudiante no tiene acceso a esa tarea.");

        var saved = await estudiantesDomain.MarcarTareaHechaAsync(estudianteId, tareaId, cancellationToken);
        return ApplicationResult.Ok(saved);
    }
    #endregion
}
