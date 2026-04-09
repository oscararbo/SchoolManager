using Back.Api.Application.Common;
using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Dtos;

namespace Back.Api.Application.Services;

public class CursosService(ICursosDomainRepository cursosDomain) : ICursosService
{
    public async Task<ApplicationResult> GetAllAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await cursosDomain.GetAllResumenAsync(cancellationToken));

    public async Task<ApplicationResult> GetSimpleAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok((await cursosDomain.GetAllResumenAsync(cancellationToken))
            .Select(c => new CursoSimpleDto { Id = c.Id, Nombre = c.Nombre }));

    public async Task<ApplicationResult> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var curso = await cursosDomain.GetDetalleAsync(id, cancellationToken);
        return curso is null
            ? ApplicationResult.NotFound("El curso no existe.")
            : ApplicationResult.Ok(curso);
    }

    public async Task<ApplicationResult> CreateAsync(CreateCursoRequestDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return ApplicationResult.BadRequest("El nombre del curso es obligatorio.");

        var result = await cursosDomain.CreateAsync(dto.Nombre.Trim(), cancellationToken);
        return ApplicationResult.Created($"/api/cursos/{result.Id}", result);
    }

    public async Task<ApplicationResult> UpdateAsync(int id, CreateCursoRequestDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return ApplicationResult.BadRequest("El nombre del curso es obligatorio.");
        if (!await cursosDomain.ExisteAsync(id, cancellationToken))
            return ApplicationResult.NotFound("El curso no existe.");

        var result = await cursosDomain.UpdateAsync(id, dto.Nombre.Trim(), cancellationToken);
        return result is null
            ? ApplicationResult.NotFound("El curso no existe.")
            : ApplicationResult.Ok(result);
    }

    public async Task<ApplicationResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!await cursosDomain.ExisteAsync(id, cancellationToken))
            return ApplicationResult.NotFound("El curso no existe.");
        if (await cursosDomain.TieneEstudiantesAsync(id, cancellationToken))
            return ApplicationResult.BadRequest("No se puede eliminar el curso porque tiene alumnos asignados.");
        if (await cursosDomain.TieneAsignaturasAsync(id, cancellationToken))
            return ApplicationResult.BadRequest("No se puede eliminar el curso porque tiene asignaturas. Eliminalas primero.");

        await cursosDomain.DeleteAsync(id, cancellationToken);
        return ApplicationResult.NoContent();
    }
}
