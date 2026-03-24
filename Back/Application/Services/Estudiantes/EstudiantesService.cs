using Back.Api.Application.Common;
using Back.Api.Application.Dtos;
using Back.Api.Domain.Repositories;
using Back.Api.Infrastructure.Security;
using System.Security.Claims;

namespace Back.Api.Application.Services;

public class EstudiantesService(IEstudiantesDomainRepository estudiantesDomain, IPasswordService passwordService) : IEstudiantesService
{
    public async Task<ApplicationResult> GetAllAsync()
        => ApplicationResult.Ok(await estudiantesDomain.GetAllAsync());

    public async Task<ApplicationResult> GetByIdAsync(int id)
    {
        var estudiante = await estudiantesDomain.GetDetalleAsync(id);
        return estudiante is null ? ApplicationResult.NotFound() : ApplicationResult.Ok(estudiante);
    }

    public async Task<ApplicationResult> CreateAsync(CreateEstudianteDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return ApplicationResult.BadRequest("El nombre del estudiante es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Correo))
            return ApplicationResult.BadRequest("El correo del estudiante es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Contrasena))
            return ApplicationResult.BadRequest("La contrasena del estudiante es obligatoria.");
        if (dto.CursoId <= 0)
            return ApplicationResult.BadRequest("El curso del estudiante es obligatorio.");

        var correo = dto.Correo.Trim().ToLowerInvariant();
        if (await estudiantesDomain.CorreoDuplicadoAsync(correo))
            return ApplicationResult.BadRequest("Ya existe un estudiante con ese correo.");
        if (!await estudiantesDomain.CursoExisteAsync(dto.CursoId))
            return ApplicationResult.BadRequest("El curso indicado no existe.");

        var result = await estudiantesDomain.CreateAsync(dto.Nombre.Trim(), correo, dto.CursoId, passwordService.Hash(dto.Contrasena.Trim()));
        return ApplicationResult.Created($"/api/estudiantes/{result.Id}", result);
    }

    public async Task<ApplicationResult> MatricularAsync(int id, int asignaturaId)
    {
        if (!await estudiantesDomain.ExisteAsync(id))
            return ApplicationResult.NotFound("El estudiante no existe.");
        if (!await estudiantesDomain.AsignaturaExisteAsync(asignaturaId))
            return ApplicationResult.NotFound("La asignatura no existe.");

        var estudianteDetalle = await estudiantesDomain.GetDetalleAsync(id);
        if (estudianteDetalle is null)
            return ApplicationResult.NotFound("El estudiante no existe.");

        if (!await estudiantesDomain.AsignaturaEsDelCursoAsync(asignaturaId, estudianteDetalle.CursoId))
            return ApplicationResult.BadRequest("El estudiante solo puede matricularse en asignaturas de su curso.");

        if (await estudiantesDomain.YaMatriculadoAsync(id, asignaturaId))
            return ApplicationResult.BadRequest("El estudiante ya esta matriculado en esta asignatura.");

        await estudiantesDomain.MatricularAsync(id, asignaturaId);
        return ApplicationResult.Ok();
    }

    public async Task<ApplicationResult> GetPanelAlumnoAsync(int id, ClaimsPrincipal user)
    {
        if (!UsuarioCoincideConEstudiante(id, user))
            return ApplicationResult.Forbidden();

        var panel = await estudiantesDomain.GetPanelAlumnoAsync(id);
        return panel is null
            ? ApplicationResult.NotFound("El estudiante no existe.")
            : ApplicationResult.Ok(panel);
    }

    public async Task<ApplicationResult> GetPanelResumenAsync(int id, ClaimsPrincipal user)
    {
        if (!UsuarioCoincideConEstudiante(id, user))
            return ApplicationResult.Forbidden();

        var panel = await estudiantesDomain.GetPanelResumenAsync(id);
        return panel is null
            ? ApplicationResult.NotFound("El estudiante no existe.")
            : ApplicationResult.Ok(panel);
    }

    public async Task<ApplicationResult> GetMateriaDetalleAsync(int id, int asignaturaId, ClaimsPrincipal user)
    {
        if (!UsuarioCoincideConEstudiante(id, user))
            return ApplicationResult.Forbidden();

        var detalle = await estudiantesDomain.GetMateriaDetalleAsync(id, asignaturaId);
        return detalle is null
            ? ApplicationResult.NotFound("La asignatura o el estudiante no existe.")
            : ApplicationResult.Ok(detalle);
    }

    public async Task<ApplicationResult> UpdateAsync(int id, UpdateEstudianteDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return ApplicationResult.BadRequest("El nombre del estudiante es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Correo))
            return ApplicationResult.BadRequest("El correo del estudiante es obligatorio.");
        if (dto.CursoId <= 0)
            return ApplicationResult.BadRequest("El curso del estudiante es obligatorio.");
        if (!await estudiantesDomain.ExisteAsync(id))
            return ApplicationResult.NotFound("El estudiante no existe.");

        var correo = dto.Correo.Trim().ToLowerInvariant();
        if (await estudiantesDomain.CorreoDuplicadoExceptAsync(correo, id))
            return ApplicationResult.BadRequest("Ya existe otro estudiante con ese correo.");
        if (!await estudiantesDomain.CursoExisteAsync(dto.CursoId))
            return ApplicationResult.BadRequest("El curso indicado no existe.");

        string? hash = string.IsNullOrWhiteSpace(dto.NuevaContrasena)
            ? null
            : passwordService.Hash(dto.NuevaContrasena.Trim());

        var result = await estudiantesDomain.UpdateAsync(id, dto.Nombre.Trim(), correo, dto.CursoId, hash);
        return result is null
            ? ApplicationResult.NotFound("El estudiante no existe.")
            : ApplicationResult.Ok(result);
    }

    public async Task<ApplicationResult> DeleteAsync(int id)
    {
        if (!await estudiantesDomain.ExisteAsync(id))
            return ApplicationResult.NotFound("El estudiante no existe.");

        await estudiantesDomain.DeleteAsync(id);
        return ApplicationResult.NoContent();
    }

    private static bool UsuarioCoincideConEstudiante(int estudianteId, ClaimsPrincipal user)
    {
        if (user.IsInRole("admin")) return true;
        var idClaim = user.FindFirstValue("id") ?? user.FindFirstValue(ClaimsIdentity.DefaultNameClaimType);
        return int.TryParse(idClaim, out var usuarioId) && usuarioId == estudianteId;
    }
}
