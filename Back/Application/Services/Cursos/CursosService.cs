using Back.Api.Application.Common;
using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Dtos;

namespace Back.Api.Application.Services;

public class CursosService(ICursosDomainRepository cursosDomain) : ICursosService
{
    public async Task<ApplicationResult> GetAllCursosAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await cursosDomain.GetAllResumenAsync(cancellationToken));

    public async Task<ApplicationResult> GetSimpleCursosAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok((await cursosDomain.GetAllResumenAsync(cancellationToken))
            .Select(c => new CursoLookupDto { Id = c.Id, Nombre = c.Nombre }));

    public async Task<ApplicationResult> GetCursoByIdAsync(int cursoId, CancellationToken cancellationToken = default)
    {
        var cursoDetalle = await cursosDomain.GetDetalleAsync(cursoId, cancellationToken);
        return cursoDetalle is null
            ? ApplicationResult.NotFound("El curso no existe.")
            : ApplicationResult.Ok(cursoDetalle);
    }

    public async Task<ApplicationResult> CreateCursoAsync(CreateCursoRequestDto createCursoRequestDto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(createCursoRequestDto.Nombre))
            return ApplicationResult.BadRequest("El nombre del curso es obligatorio.");

        var createdCurso = await cursosDomain.CreateCursoAsync(createCursoRequestDto.Nombre.Trim(), cancellationToken);
        return ApplicationResult.Created($"/api/cursos/{createdCurso.Id}", createdCurso);
    }

    public async Task<ApplicationResult> UpdateCursoAsync(int cursoId, CreateCursoRequestDto updateCursoRequestDto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(updateCursoRequestDto.Nombre))
            return ApplicationResult.BadRequest("El nombre del curso es obligatorio.");
        if (!await cursosDomain.ExisteAsync(cursoId, cancellationToken))
            return ApplicationResult.NotFound("El curso no existe.");

        var updatedCurso = await cursosDomain.UpdateCursoAsync(cursoId, updateCursoRequestDto.Nombre.Trim(), cancellationToken);
        return updatedCurso is null
            ? ApplicationResult.NotFound("El curso no existe.")
            : ApplicationResult.Ok(updatedCurso);
    }

    public async Task<ApplicationResult> DeleteCursoAsync(int cursoId, CancellationToken cancellationToken = default)
    {
        if (!await cursosDomain.ExisteAsync(cursoId, cancellationToken))
            return ApplicationResult.NotFound("El curso no existe.");
        if (await cursosDomain.TieneEstudiantesAsync(cursoId, cancellationToken))
            return ApplicationResult.BadRequest("No se puede eliminar el curso porque tiene alumnos asignados.");
        if (await cursosDomain.TieneAsignaturasAsync(cursoId, cancellationToken))
            return ApplicationResult.BadRequest("No se puede eliminar el curso porque tiene asignaturas. Eliminalas primero.");

        await cursosDomain.DeleteCursoAsync(cursoId, cancellationToken);
        return ApplicationResult.NoContent();
    }
}
