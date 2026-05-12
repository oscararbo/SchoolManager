using Back.Api.Application.Common;
using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Abstractions.Security;
using Back.Api.Application.Configuration;
using Back.Api.Application.Dtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Back.Api.Application.Services;

public class EstudiantesService(IEstudiantesDomainRepository estudiantesDomain, IPasswordService passwordService, ICurrentSchoolContext currentSchoolContext, IWebHostEnvironment hostEnvironment) : IEstudiantesService
{
    public async Task<ApplicationResult> GetAllEstudiantesAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await estudiantesDomain.GetAllEstudiantesAsync(cancellationToken));

    public async Task<ApplicationResult> GetSimpleEstudiantesAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await estudiantesDomain.GetSimpleEstudiantesAsync(cancellationToken));

    public async Task<ApplicationResult> GetEstudianteByIdAsync(int estudianteId, CancellationToken cancellationToken = default)
    {
        var estudiante = await estudiantesDomain.GetDetalleAsync(estudianteId, cancellationToken);
        return estudiante is null ? ApplicationResult.NotFound("El estudiante no existe.") : ApplicationResult.Ok(estudiante);
    }

    public async Task<ApplicationResult> CreateEstudianteAsync(CreateEstudianteRequestDto createEstudianteRequestDto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(createEstudianteRequestDto.Nombre))
            return ApplicationResult.BadRequest("El nombre del estudiante es obligatorio.");
        if (createEstudianteRequestDto.CursoId <= 0)
            return ApplicationResult.BadRequest("El curso del estudiante es obligatorio.");

        var normalizedDocumento = CredentialGenerationHelper.NormalizeDniNie(createEstudianteRequestDto.DNI);
        if (!CredentialGenerationHelper.IsValidDniNie(normalizedDocumento))
            return ApplicationResult.BadRequest("El documento debe ser un DNI o NIE valido.");
        if (await estudiantesDomain.DocumentoDuplicadoAsync(normalizedDocumento, cancellationToken))
            return ApplicationResult.BadRequest("Ya existe una persona con ese DNI/NIE.");
        if (!await estudiantesDomain.CursoExisteAsync(createEstudianteRequestDto.CursoId, cancellationToken))
            return ApplicationResult.BadRequest("El curso indicado no existe.");

        var schoolSlug = CredentialGenerationHelper.NormalizeSchoolSlugForDomain(currentSchoolContext.SchoolSlug, currentSchoolContext.SchoolId);
        var generatedPassword = CredentialGenerationHelper.GeneratePassword();
        var generatedEmail = await GenerateUniqueEmailAsync($"{createEstudianteRequestDto.Nombre} {createEstudianteRequestDto.Apellidos}", "alumno", schoolSlug, cancellationToken);

        var createdEstudiante = await estudiantesDomain.CreateEstudianteAsync(createEstudianteRequestDto.Nombre.Trim(), generatedEmail, createEstudianteRequestDto.CursoId, passwordService.Hash(generatedPassword), createEstudianteRequestDto.Apellidos.Trim(), normalizedDocumento, createEstudianteRequestDto.Telefono.Trim(), createEstudianteRequestDto.FechaNacimiento!.Value, cancellationToken);
        createdEstudiante.ContrasenaTemporal = generatedPassword;
        return ApplicationResult.Created($"/api/estudiantes/{createdEstudiante.Id}", createdEstudiante);
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

    public async Task<ApplicationResult> GetMateriaDetalleAsync(int estudianteId, int asignaturaId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConEstudiante(estudianteId, user))
            return ApplicationResult.Forbidden();

        var detail = await estudiantesDomain.GetMateriaDetalleAsync(estudianteId, asignaturaId, cancellationToken);
        return detail is null
            ? ApplicationResult.NotFound("La asignatura o el estudiante no existe.")
            : ApplicationResult.Ok(detail);
    }

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

    private async Task<string> GenerateUniqueEmailAsync(string fullName, string rolePrefix, string schoolSlug, CancellationToken cancellationToken)
    {
        for (var i = 0; i < 2000; i++)
        {
            var candidate = CredentialGenerationHelper.BuildGeneratedEmail(fullName, rolePrefix, schoolSlug, i);
            if (!await estudiantesDomain.CorreoDuplicadoAsync(candidate, cancellationToken))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("No se pudo generar un correo unico para el estudiante.");
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

    public async Task<ApplicationResult> SubirSubmisionAsync(int estudianteId, int tareaId, IFormFile archivo, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!UsuarioCoincideConEstudiante(estudianteId, user))
            return ApplicationResult.Forbidden();

        if (archivo is null || archivo.Length == 0)
            return ApplicationResult.BadRequest("Debes adjuntar un archivo.");

        if (archivo.Length > 10 * 1024 * 1024)
            return ApplicationResult.BadRequest("El archivo supera el tamano maximo permitido (10MB).");

        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".txt", ".jpg", ".jpeg", ".png"
        };
        var extension = Path.GetExtension(archivo.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
            return ApplicationResult.BadRequest("Tipo de archivo no permitido.");

        if (!await estudiantesDomain.EstudianteMatriculadoEnTareaAsync(estudianteId, tareaId, cancellationToken))
            return ApplicationResult.BadRequest("El estudiante no tiene acceso a esa tarea.");

        var uploadsRoot = Path.Combine(hostEnvironment.ContentRootPath, "uploads", "tareas", tareaId.ToString());
        Directory.CreateDirectory(uploadsRoot);

        var safeBaseName = Path.GetFileNameWithoutExtension(archivo.FileName);
        safeBaseName = string.Concat(safeBaseName.Where(ch => char.IsLetterOrDigit(ch) || ch == '-' || ch == '_'));
        if (string.IsNullOrWhiteSpace(safeBaseName)) safeBaseName = "archivo";

        var generatedFileName = $"{estudianteId}_{DateTime.UtcNow:yyyyMMddHHmmss}_{safeBaseName}{extension}";
        var absolutePath = Path.Combine(uploadsRoot, generatedFileName);

        await using (var stream = File.Create(absolutePath))
        {
            await archivo.CopyToAsync(stream, cancellationToken);
        }

        var relativePath = $"/uploads/tareas/{tareaId}/{generatedFileName}";
        var saved = await estudiantesDomain.UpsertSubmisionEstudianteAsync(estudianteId, tareaId, archivo.FileName, relativePath, archivo.Length, cancellationToken);
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
}
