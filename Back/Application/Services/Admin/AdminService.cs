using Back.Api.Application.Common;
using Back.Api.Application.Dtos;
using Back.Api.Domain.Repositories;
using Back.Api.Infrastructure.Security;
using System.Security.Claims;

namespace Back.Api.Application.Services;

public class AdminService(IAdminDomainRepository adminDomain, IPasswordService passwordService) : IAdminService
{
    public async Task<ApplicationResult> GetAllAsync()
        => ApplicationResult.Ok(await adminDomain.GetAllAsync());

    public async Task<ApplicationResult> CreateAsync(CreateAdminDto dto, ClaimsPrincipal user)
    {
        if (!user.IsInRole("admin"))
            return ApplicationResult.Forbidden();
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return ApplicationResult.BadRequest("El nombre del administrador es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Correo))
            return ApplicationResult.BadRequest("El correo del administrador es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Contrasena))
            return ApplicationResult.BadRequest("La contrasena del administrador es obligatoria.");

        var correo = dto.Correo.Trim().ToLowerInvariant();
        if (await adminDomain.CorreoDuplicadoAsync(correo))
            return ApplicationResult.BadRequest("Ya existe un administrador con ese correo.");

        var result = await adminDomain.CreateAsync(dto.Nombre.Trim(), correo, passwordService.Hash(dto.Contrasena.Trim()));
        return ApplicationResult.Created($"/api/admin/{result.Id}", result);
    }
}
