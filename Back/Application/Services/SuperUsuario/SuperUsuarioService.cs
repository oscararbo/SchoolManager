using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Abstractions.Security;
using Back.Api.Application.Common;
using Back.Api.Application.Dtos;

namespace Back.Api.Application.Services;

public class SuperUsuarioService(
    ISuperUsuarioDomainRepository superUsuarioDomain,
    IPasswordService passwordService) : ISuperUsuarioService
{
    public async Task<ApplicationResult> GetColegiosAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await superUsuarioDomain.GetColegiosAsync(cancellationToken));

    public async Task<ApplicationResult> GetAdminsByColegioAsync(int colegioId, CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await superUsuarioDomain.GetAdminsByColegioAsync(colegioId, cancellationToken));

    public async Task<ApplicationResult> GetColegioBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = NormalizeSlug(slug);
        var colegio = await superUsuarioDomain.GetColegioBySlugAsync(normalizedSlug, cancellationToken);
        return colegio is null
            ? ApplicationResult.NotFound("Colegio no encontrado.")
            : ApplicationResult.Ok(colegio);
    }

    public async Task<ApplicationResult> CreateColegioAsync(CreateColegioRequestDto request, CancellationToken cancellationToken = default)
    {
        var nombre = request.Nombre.Trim();
        var slug = NormalizeSlug(request.Slug);

        if (await superUsuarioDomain.ColegioSlugExistsAsync(slug, null, cancellationToken))
            return ApplicationResult.BadRequest("Ya existe un colegio con ese slug.");

        var created = await superUsuarioDomain.CreateColegioAsync(nombre, slug, request.LogoUrl, request.FaviconUrl, request.ColorPrimario, request.MensajeLogin, cancellationToken);
        return ApplicationResult.Created($"/api/superusuario/colegios/{created.Id}", created);
    }

    public async Task<ApplicationResult> UpdateColegioAsync(int colegioId, UpdateColegioRequestDto request, CancellationToken cancellationToken = default)
    {
        var nombre = request.Nombre.Trim();
        var slug = NormalizeSlug(request.Slug);

        if (await superUsuarioDomain.ColegioSlugExistsAsync(slug, colegioId, cancellationToken))
            return ApplicationResult.BadRequest("Ya existe otro colegio con ese slug.");

        var updated = await superUsuarioDomain.UpdateColegioAsync(colegioId, nombre, slug, request.LogoUrl, request.FaviconUrl, request.ColorPrimario, request.MensajeLogin, cancellationToken);
        return updated is null
            ? ApplicationResult.NotFound("Colegio no encontrado.")
            : ApplicationResult.Ok(updated);
    }

    public async Task<ApplicationResult> DeleteColegioAsync(int colegioId, CancellationToken cancellationToken = default)
    {
        var deleted = await superUsuarioDomain.DeleteColegioAsync(colegioId, cancellationToken);
        return deleted
            ? ApplicationResult.NoContent()
            : ApplicationResult.NotFound("Colegio no encontrado.");
    }

    public async Task<ApplicationResult> CreateAdminColegioAsync(int colegioId, CreateAdminColegioRequestDto request, CancellationToken cancellationToken = default)
    {
        var correo = request.Correo.Trim().ToLowerInvariant();
        if (await superUsuarioDomain.ColegioCorreoDuplicadoAsync(colegioId, correo, cancellationToken))
            return ApplicationResult.BadRequest("Ya existe una cuenta con ese correo en este colegio.");

        var created = await superUsuarioDomain.CreateAdminColegioAsync(
            colegioId,
            request.Nombre.Trim(),
            correo,
            passwordService.Hash(request.Contrasena.Trim()),
            cancellationToken);

        return ApplicationResult.Created($"/api/superusuario/colegios/{colegioId}/admins/{created.Id}", created);
    }

    private static string NormalizeSlug(string slug)
        => slug.Trim().ToLowerInvariant();
}
