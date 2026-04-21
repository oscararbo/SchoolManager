using Back.Api.Application.Common;
using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Abstractions.Security;
using Back.Api.Application.Configuration;
using Back.Api.Application.Dtos;
using System.Security.Claims;

namespace Back.Api.Application.Services;

public class AdminService(
    IAdminDomainRepository adminDomain,
    IPasswordService passwordService) : IAdminService
{
    public async Task<ApplicationResult> GetAllAdminsAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await adminDomain.GetAllAdminsAsync(cancellationToken));

    public async Task<ApplicationResult> GetStatsAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await adminDomain.GetStatsAsync(cancellationToken));

    public async Task<ApplicationResult> GetCursosStatsSelectorAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await adminDomain.GetCursosStatsSelectorAsync(cancellationToken));

    public async Task<ApplicationResult> GetStatsByCursoAsync(int cursoId, CancellationToken cancellationToken = default)
    {
        var cursoStats = await adminDomain.GetStatsByCursoAsync(cursoId, cancellationToken);
        if (cursoStats is null)
            return ApplicationResult.NotFound("El curso no existe.");

        return ApplicationResult.Ok(cursoStats);
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

    public async Task<ApplicationResult> CreateAdminAsync(CreateAdminRequestDto createAdminRequestDto, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!user.IsInRole(Roles.Admin))
            return ApplicationResult.Forbidden("No tienes permisos para crear administradores.");

        var correo = createAdminRequestDto.Correo.Trim().ToLowerInvariant();
        if (await adminDomain.CorreoDuplicadoAsync(correo, cancellationToken))
            return ApplicationResult.BadRequest("Ya existe un administrador con ese correo.");

        var createdAdmin = await adminDomain.CreateAdminAsync(createAdminRequestDto.Nombre.Trim(), correo, passwordService.Hash(createAdminRequestDto.Contrasena.Trim()), cancellationToken);
        return ApplicationResult.Created($"/api/admin/{createdAdmin.Id}", createdAdmin);
    }
}
