using Back.Api.Application.Common;
using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Abstractions.Security;
using Back.Api.Application.Configuration;
using Back.Api.Application.Dtos;
using System.Security.Claims;

namespace Back.Api.Application.Services;

public class EstudiantesService(IEstudiantesDomainRepository estudiantesDomain, IPasswordService passwordService) : IEstudiantesService
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
        if (string.IsNullOrWhiteSpace(createEstudianteRequestDto.Correo))
            return ApplicationResult.BadRequest("El correo del estudiante es obligatorio.");
        if (string.IsNullOrWhiteSpace(createEstudianteRequestDto.Contrasena))
            return ApplicationResult.BadRequest("La contrasena del estudiante es obligatoria.");
        if (createEstudianteRequestDto.CursoId <= 0)
            return ApplicationResult.BadRequest("El curso del estudiante es obligatorio.");

        var correo = createEstudianteRequestDto.Correo.Trim().ToLowerInvariant();
        if (await estudiantesDomain.CorreoDuplicadoAsync(correo, cancellationToken))
            return ApplicationResult.BadRequest("Ya existe un estudiante con ese correo.");
        if (!await estudiantesDomain.CursoExisteAsync(createEstudianteRequestDto.CursoId, cancellationToken))
            return ApplicationResult.BadRequest("El curso indicado no existe.");

        var createdEstudiante = await estudiantesDomain.CreateEstudianteAsync(createEstudianteRequestDto.Nombre.Trim(), correo, createEstudianteRequestDto.CursoId, passwordService.Hash(createEstudianteRequestDto.Contrasena.Trim()), createEstudianteRequestDto.Apellidos.Trim(), createEstudianteRequestDto.DNI.Trim().ToUpperInvariant(), createEstudianteRequestDto.Telefono.Trim(), createEstudianteRequestDto.FechaNacimiento!.Value, cancellationToken);
        return ApplicationResult.Created($"/api/estudiantes/{createdEstudiante.Id}", createdEstudiante);
    }

    public async Task<ApplicationResult> MatricularAsync(int estudianteId, int asignaturaId, CancellationToken cancellationToken = default)
    {
        if (!await estudiantesDomain.ExisteAsync(estudianteId, cancellationToken))
            return ApplicationResult.NotFound("El estudiante no existe.");
        if (!await estudiantesDomain.AsignaturaExisteAsync(asignaturaId, cancellationToken))
            return ApplicationResult.NotFound("La asignatura no existe.");

        var estudianteDetalle = await estudiantesDomain.GetDetalleAsync(estudianteId, cancellationToken);
        if (estudianteDetalle is null)
            return ApplicationResult.NotFound("El estudiante no existe.");

        if (!await estudiantesDomain.AsignaturaEsDelCursoAsync(asignaturaId, estudianteDetalle.CursoId, cancellationToken))
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

        var detalle = await estudiantesDomain.GetMateriaDetalleAsync(estudianteId, asignaturaId, cancellationToken);
        return detalle is null
            ? ApplicationResult.NotFound("La asignatura o el estudiante no existe.")
            : ApplicationResult.Ok(detalle);
    }

    public async Task<ApplicationResult> UpdateEstudianteAsync(int estudianteId, UpdateEstudianteRequestDto updateEstudianteRequestDto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(updateEstudianteRequestDto.Nombre))
            return ApplicationResult.BadRequest("El nombre del estudiante es obligatorio.");
        if (string.IsNullOrWhiteSpace(updateEstudianteRequestDto.Correo))
            return ApplicationResult.BadRequest("El correo del estudiante es obligatorio.");
        if (updateEstudianteRequestDto.CursoId <= 0)
            return ApplicationResult.BadRequest("El curso del estudiante es obligatorio.");
        if (!await estudiantesDomain.ExisteAsync(estudianteId, cancellationToken))
            return ApplicationResult.NotFound("El estudiante no existe.");

        var correo = updateEstudianteRequestDto.Correo.Trim().ToLowerInvariant();
        if (await estudiantesDomain.CorreoDuplicadoExceptAsync(correo, estudianteId, cancellationToken))
            return ApplicationResult.BadRequest("Ya existe otro estudiante con ese correo.");
        if (!await estudiantesDomain.CursoExisteAsync(updateEstudianteRequestDto.CursoId, cancellationToken))
            return ApplicationResult.BadRequest("El curso indicado no existe.");

        string? contrasenaHash = string.IsNullOrWhiteSpace(updateEstudianteRequestDto.NuevaContrasena)
            ? null
            : passwordService.Hash(updateEstudianteRequestDto.NuevaContrasena.Trim());

        var updatedEstudiante = await estudiantesDomain.UpdateEstudianteAsync(estudianteId, updateEstudianteRequestDto.Nombre.Trim(), correo, updateEstudianteRequestDto.CursoId, contrasenaHash, updateEstudianteRequestDto.Apellidos.Trim(), updateEstudianteRequestDto.DNI.Trim().ToUpperInvariant(), updateEstudianteRequestDto.Telefono.Trim(), updateEstudianteRequestDto.FechaNacimiento!.Value, cancellationToken);
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
}
