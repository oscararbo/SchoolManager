using Back.Api.Application.Common;
using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Abstractions.Security;
using Back.Api.Application.Dtos;
using System.Security.Claims;

namespace Back.Api.Application.Services;

public class AdminService(
    IAdminDomainRepository adminDomain,
    IPasswordService passwordService) : IAdminService
{
    public async Task<ApplicationResult> GetAllAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await adminDomain.GetAllAsync(cancellationToken));

    public async Task<ApplicationResult> GetStatsAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await adminDomain.GetStatsAsync(cancellationToken));

    public async Task<ApplicationResult> GetCursosStatsSelectorAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await adminDomain.GetCursosStatsSelectorAsync(cancellationToken));

    public async Task<ApplicationResult> GetStatsByCursoAsync(int cursoId, CancellationToken cancellationToken = default)
    {
        var result = await adminDomain.GetStatsByCursoAsync(cursoId, cancellationToken);
        if (result is null)
            return ApplicationResult.NotFound("El curso no existe.");

        return ApplicationResult.Ok(result);
    }

    public async Task<ApplicationResult> CompareCursosAsync(IEnumerable<int> cursoIds, CancellationToken cancellationToken = default)
    {
        var ids = cursoIds
            .Where(id => id > 0)
            .Distinct()
            .Take(6)
            .ToList();

        if (ids.Count < 2)
            return ApplicationResult.BadRequest("Selecciona al menos 2 cursos para comparar.");

        return ApplicationResult.Ok(new ComparacionCursosResponseDto
        {
            Cursos = (await adminDomain.CompareCursosAsync(ids, cancellationToken)).ToList()
        });
    }

    public async Task<ApplicationResult> GetMatriculasAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await adminDomain.GetMatriculasAsync(cancellationToken));

    public async Task<ApplicationResult> GetImparticionesAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await adminDomain.GetImparticionesAsync(cancellationToken));

    public async Task<ApplicationResult> CreateAsync(CreateAdminRequestDto dto, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        _ = user;

        var correo = dto.Correo.Trim().ToLowerInvariant();
        if (await adminDomain.CorreoDuplicadoAsync(correo, cancellationToken))
            return ApplicationResult.BadRequest("Ya existe un administrador con ese correo.");

        var result = await adminDomain.CreateAsync(dto.Nombre.Trim(), correo, passwordService.Hash(dto.Contrasena.Trim()), cancellationToken);
        return ApplicationResult.Created($"/api/admin/{result.Id}", result);
    }
}
