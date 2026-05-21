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

    public async Task<ApplicationResult> GetMatriculasAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await adminDomain.GetMatriculasAsync(cancellationToken));

    public async Task<ApplicationResult> GetImparticionesAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await adminDomain.GetImparticionesAsync(cancellationToken));

    public async Task<ApplicationResult> CreateAdminAsync(CreateAdminRequestDto createAdminRequestDto, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!user.IsInRole(Roles.Admin))
            return ApplicationResult.Forbidden("No tienes permisos para crear administradores.");

        var email = createAdminRequestDto.Correo.Trim().ToLowerInvariant();
        if (await adminDomain.CorreoDuplicadoAsync(email, cancellationToken))
            return ApplicationResult.BadRequest("Ya existe un administrador con ese email.");

        var createdAdmin = await adminDomain.CreateAdminAsync(createAdminRequestDto.Nombre.Trim(), email, passwordService.Hash(createAdminRequestDto.Contrasena.Trim()), cancellationToken);
        return ApplicationResult.Created($"/api/admin/{createdAdmin.Id}", createdAdmin);
    }
}
