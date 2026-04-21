using Back.Api.Application.Common;
using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Dtos;

namespace Back.Api.Application.Services;

public class AsignaturasService(IAsignaturasDomainRepository asignaturasDomain) : IAsignaturasService
{
    public async Task<ApplicationResult> GetAllAsignaturasAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await asignaturasDomain.GetAllResumenAsync(cancellationToken));

    public async Task<ApplicationResult> GetSimpleAsignaturasAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok((await asignaturasDomain.GetAllResumenAsync(cancellationToken))
            .Select(a => new AsignaturaLookupDto
            {
                Id = a.Id,
                Nombre = a.Nombre,
                CursoId = a.Curso.Id,
                Curso = a.Curso.Nombre
            }));

    public async Task<ApplicationResult> GetAsignaturaByIdAsync(int asignaturaId, CancellationToken cancellationToken = default)
    {
        var asignatura = await asignaturasDomain.GetDetalleAsync(asignaturaId, cancellationToken);
        return asignatura is null ? ApplicationResult.NotFound("La asignatura no existe.") : ApplicationResult.Ok(asignatura);
    }

    public async Task<ApplicationResult> CreateAsignaturaAsync(CreateAsignaturaRequestDto createAsignaturaRequestDto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(createAsignaturaRequestDto.Nombre))
            return ApplicationResult.BadRequest("El nombre de la asignatura es obligatorio.");
        if (!await asignaturasDomain.CursoExisteAsync(createAsignaturaRequestDto.CursoId, cancellationToken))
            return ApplicationResult.BadRequest("El curso indicado no existe.");

        var nombreAsignatura = createAsignaturaRequestDto.Nombre.Trim();
        if (await asignaturasDomain.ExisteEnCursoAsync(createAsignaturaRequestDto.CursoId, nombreAsignatura, cancellationToken))
            return ApplicationResult.BadRequest("Ya existe esa asignatura en ese curso.");

        var createdAsignatura = await asignaturasDomain.CreateAsignaturaAsync(nombreAsignatura, createAsignaturaRequestDto.CursoId, cancellationToken);
        return ApplicationResult.Created($"/api/asignaturas/{createdAsignatura.Id}", createdAsignatura);
    }

    public async Task<ApplicationResult> UpdateAsignaturaAsync(int asignaturaId, CreateAsignaturaRequestDto updateAsignaturaRequestDto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(updateAsignaturaRequestDto.Nombre))
            return ApplicationResult.BadRequest("El nombre de la asignatura es obligatorio.");
        if (!await asignaturasDomain.ExisteAsync(asignaturaId, cancellationToken))
            return ApplicationResult.NotFound("La asignatura no existe.");
        if (!await asignaturasDomain.CursoExisteAsync(updateAsignaturaRequestDto.CursoId, cancellationToken))
            return ApplicationResult.BadRequest("El curso indicado no existe.");

        var updatedAsignatura = await asignaturasDomain.UpdateAsignaturaAsync(asignaturaId, updateAsignaturaRequestDto.Nombre.Trim(), updateAsignaturaRequestDto.CursoId, cancellationToken);
        return updatedAsignatura is null ? ApplicationResult.NotFound("La asignatura no existe.") : ApplicationResult.Ok(updatedAsignatura);
    }

    public async Task<ApplicationResult> DeleteAsignaturaAsync(int asignaturaId, CancellationToken cancellationToken = default)
    {
        if (!await asignaturasDomain.ExisteAsync(asignaturaId, cancellationToken))
            return ApplicationResult.NotFound("La asignatura no existe.");

        await asignaturasDomain.DeleteAsignaturaAsync(asignaturaId, cancellationToken);
        return ApplicationResult.NoContent();
    }
}
