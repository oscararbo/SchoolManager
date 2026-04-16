using Back.Api.Application.Common;
using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Dtos;

namespace Back.Api.Application.Services;

public class AsignaturasService(IAsignaturasDomainRepository asignaturasDomain) : IAsignaturasService
{
    public async Task<ApplicationResult> GetAllAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await asignaturasDomain.GetAllResumenAsync(cancellationToken));

    public async Task<ApplicationResult> GetSimpleAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok((await asignaturasDomain.GetAllResumenAsync(cancellationToken))
            .Select(a => new AsignaturaLookupDto
            {
                Id = a.Id,
                Nombre = a.Nombre,
                CursoId = a.Curso.Id,
                Curso = a.Curso.Nombre
            }));

    public async Task<ApplicationResult> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var asignatura = await asignaturasDomain.GetDetalleAsync(id, cancellationToken);
        return asignatura is null ? ApplicationResult.NotFound("La asignatura no existe.") : ApplicationResult.Ok(asignatura);
    }

    public async Task<ApplicationResult> CreateAsync(CreateAsignaturaRequestDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return ApplicationResult.BadRequest("El nombre de la asignatura es obligatorio.");
        if (!await asignaturasDomain.CursoExisteAsync(dto.CursoId, cancellationToken))
            return ApplicationResult.BadRequest("El curso indicado no existe.");

        var nombre = dto.Nombre.Trim();
        if (await asignaturasDomain.ExisteEnCursoAsync(dto.CursoId, nombre, cancellationToken))
            return ApplicationResult.BadRequest("Ya existe esa asignatura en ese curso.");

        var result = await asignaturasDomain.CreateAsync(nombre, dto.CursoId, cancellationToken);
        return ApplicationResult.Created($"/api/asignaturas/{result.Id}", result);
    }

    public async Task<ApplicationResult> UpdateAsync(int id, CreateAsignaturaRequestDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return ApplicationResult.BadRequest("El nombre de la asignatura es obligatorio.");
        if (!await asignaturasDomain.ExisteAsync(id, cancellationToken))
            return ApplicationResult.NotFound("La asignatura no existe.");
        if (!await asignaturasDomain.CursoExisteAsync(dto.CursoId, cancellationToken))
            return ApplicationResult.BadRequest("El curso indicado no existe.");

        var result = await asignaturasDomain.UpdateAsync(id, dto.Nombre.Trim(), dto.CursoId, cancellationToken);
        return result is null ? ApplicationResult.NotFound("La asignatura no existe.") : ApplicationResult.Ok(result);
    }

    public async Task<ApplicationResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!await asignaturasDomain.ExisteAsync(id, cancellationToken))
            return ApplicationResult.NotFound("La asignatura no existe.");

        await asignaturasDomain.DeleteAsync(id, cancellationToken);
        return ApplicationResult.NoContent();
    }
}
