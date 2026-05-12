using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Abstractions.Security;
using Back.Api.Application.Common;
using Back.Api.Application.Dtos;
using Back.Api.Application.Dtos.SuperUsuario.Requests;
using Microsoft.AspNetCore.Hosting;

namespace Back.Api.Application.Services;

public class SuperUsuarioService(
    ISuperUsuarioDomainRepository superUsuarioDomain,
    IPasswordService passwordService,
    IWebHostEnvironment hostEnvironment) : ISuperUsuarioService
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
        var colegio = await superUsuarioDomain.GetColegioByIdAsync(colegioId, cancellationToken);
        if (colegio is null)
            return ApplicationResult.NotFound("Colegio no encontrado.");

        var schoolSlug = CredentialGenerationHelper.NormalizeSchoolSlugForDomain(colegio.Slug, colegio.Id);
        var generatedPassword = CredentialGenerationHelper.GeneratePassword();
        var generatedEmail = await GenerateUniqueEmailAsync($"{request.Nombre}", "admin", schoolSlug, colegioId, cancellationToken);

        var created = await superUsuarioDomain.CreateAdminColegioAsync(
            colegioId,
            request.Nombre.Trim(),
            generatedEmail,
            passwordService.Hash(generatedPassword),
            cancellationToken);

        created.ContrasenaTemporal = generatedPassword;

        return ApplicationResult.Created($"/api/superusuario/colegios/{colegioId}/admins/{created.Id}", created);
    }

    public async Task<ApplicationResult> UpdateColegioImagenAsync(int colegioId, UpdateColegioImagenRequestDto request, CancellationToken cancellationToken = default)
    {
        var tipo = (request.TipoImagen ?? string.Empty).Trim().ToLowerInvariant();
        if (tipo is not ("logo" or "favicon"))
            return ApplicationResult.BadRequest("Tipo de imagen invalido. Usa 'logo' o 'favicon'.");

        if (request.ImagenArchivo is null || request.ImagenArchivo.Length == 0)
            return ApplicationResult.BadRequest("Debes adjuntar un archivo de imagen.");

        if (request.ImagenArchivo.Length > 5 * 1024 * 1024)
            return ApplicationResult.BadRequest("La imagen supera el tamano maximo permitido (5MB).");

        var extension = Path.GetExtension(request.ImagenArchivo.FileName).ToLowerInvariant();
        var allowedExtensions = new HashSet<string> { ".png", ".jpg", ".jpeg", ".svg", ".ico", ".webp" };
        if (!allowedExtensions.Contains(extension))
            return ApplicationResult.BadRequest("Formato no permitido. Usa png, jpg, jpeg, svg, ico o webp.");

        var uploadsRoot = Path.Combine(hostEnvironment.ContentRootPath, "uploads", "colegios", colegioId.ToString());
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"{tipo}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
        var fullPath = Path.Combine(uploadsRoot, fileName);
        await using (var stream = File.Create(fullPath))
        {
            await request.ImagenArchivo.CopyToAsync(stream, cancellationToken);
        }

        var relativePath = $"/uploads/colegios/{colegioId}/{fileName}";
        var updated = await superUsuarioDomain.UpdateColegioImagenUrlAsync(colegioId, tipo, relativePath, cancellationToken);
        return updated is null
            ? ApplicationResult.NotFound("Colegio no encontrado.")
            : ApplicationResult.Ok(updated);
    }

    private async Task<string> GenerateUniqueEmailAsync(string fullName, string rolePrefix, string schoolSlug, int colegioId, CancellationToken cancellationToken)
    {
        for (var i = 0; i < 2000; i++)
        {
            var candidate = CredentialGenerationHelper.BuildGeneratedEmail(fullName, rolePrefix, schoolSlug, i);
            if (!await superUsuarioDomain.ColegioCorreoDuplicadoAsync(colegioId, candidate, cancellationToken))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("No se pudo generar un correo unico para el administrador del colegio.");
    }

    private static string NormalizeSlug(string slug)
        => slug.Trim().ToLowerInvariant();
}
