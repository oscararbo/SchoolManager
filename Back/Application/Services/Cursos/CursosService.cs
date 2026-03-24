using Back.Api.Application.Common;
using Back.Api.Application.Dtos;
using Back.Api.Domain.Repositories;

namespace Back.Api.Application.Services;

public class CursosService(ICursosDomainRepository cursosDomain) : ICursosService
{
    public async Task<ApplicationResult> GetAllAsync()
        => ApplicationResult.Ok(await cursosDomain.GetAllResumenAsync());

    public async Task<ApplicationResult> GetByIdAsync(int id)
    {
        var curso = await cursosDomain.GetDetalleAsync(id);
        return curso is null
            ? ApplicationResult.NotFound("El curso no existe.")
            : ApplicationResult.Ok(curso);
    }

    public async Task<ApplicationResult> CreateAsync(CreateCursoDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return ApplicationResult.BadRequest("El nombre del curso es obligatorio.");

        var result = await cursosDomain.CreateAsync(dto.Nombre.Trim());
        return ApplicationResult.Created($"/api/cursos/{result.Id}", result);
    }

    public async Task<ApplicationResult> UpdateAsync(int id, UpdateCursoDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return ApplicationResult.BadRequest("El nombre del curso es obligatorio.");
        if (!await cursosDomain.ExisteAsync(id))
            return ApplicationResult.NotFound("El curso no existe.");

        var result = await cursosDomain.UpdateAsync(id, dto.Nombre.Trim());
        return result is null
            ? ApplicationResult.NotFound("El curso no existe.")
            : ApplicationResult.Ok(result);
    }

    public async Task<ApplicationResult> DeleteAsync(int id)
    {
        if (!await cursosDomain.ExisteAsync(id))
            return ApplicationResult.NotFound("El curso no existe.");
        if (await cursosDomain.TieneEstudiantesAsync(id))
            return ApplicationResult.BadRequest("No se puede eliminar el curso porque tiene alumnos asignados.");
        if (await cursosDomain.TieneAsignaturasAsync(id))
            return ApplicationResult.BadRequest("No se puede eliminar el curso porque tiene asignaturas. Eliminalas primero.");

        await cursosDomain.DeleteAsync(id);
        return ApplicationResult.NoContent();
    }
}
