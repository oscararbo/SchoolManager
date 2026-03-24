using Back.Api.Application.Common;
using Back.Api.Application.Dtos;
using Back.Api.Domain.Repositories;

namespace Back.Api.Application.Services;

public class AsignaturasService(IAsignaturasDomainRepository asignaturasDomain) : IAsignaturasService
{
    public async Task<ApplicationResult> GetAllAsync()
        => ApplicationResult.Ok(await asignaturasDomain.GetAllResumenAsync());

    public async Task<ApplicationResult> GetByIdAsync(int id)
    {
        var asignatura = await asignaturasDomain.GetDetalleAsync(id);
        return asignatura is null ? ApplicationResult.NotFound() : ApplicationResult.Ok(asignatura);
    }

    public async Task<ApplicationResult> CreateAsync(CreateAsignaturaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return ApplicationResult.BadRequest("El nombre de la asignatura es obligatorio.");
        if (!await asignaturasDomain.CursoExisteAsync(dto.CursoId))
            return ApplicationResult.BadRequest("El curso indicado no existe.");

        var nombre = dto.Nombre.Trim();
        if (await asignaturasDomain.ExisteEnCursoAsync(dto.CursoId, nombre))
            return ApplicationResult.BadRequest("Ya existe esa asignatura en ese curso.");

        var result = await asignaturasDomain.CreateAsync(nombre, dto.CursoId);
        return ApplicationResult.Created($"/api/asignaturas/{result.Id}", result);
    }

    public async Task<ApplicationResult> UpdateAsync(int id, UpdateAsignaturaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return ApplicationResult.BadRequest("El nombre de la asignatura es obligatorio.");
        if (!await asignaturasDomain.ExisteAsync(id))
            return ApplicationResult.NotFound("La asignatura no existe.");
        if (!await asignaturasDomain.CursoExisteAsync(dto.CursoId))
            return ApplicationResult.BadRequest("El curso indicado no existe.");

        var result = await asignaturasDomain.UpdateAsync(id, dto.Nombre.Trim(), dto.CursoId);
        return result is null ? ApplicationResult.NotFound("La asignatura no existe.") : ApplicationResult.Ok(result);
    }

    public async Task<ApplicationResult> DeleteAsync(int id)
    {
        if (!await asignaturasDomain.ExisteAsync(id))
            return ApplicationResult.NotFound("La asignatura no existe.");

        await asignaturasDomain.DeleteAsync(id);
        return ApplicationResult.NoContent();
    }
}
