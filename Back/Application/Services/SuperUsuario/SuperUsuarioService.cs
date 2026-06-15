using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Abstractions.Security;
using Back.Api.Application.Common;
using Back.Api.Application.Dtos;
using Back.Api.Application.Dtos.SuperUsuario.Requests;
using Back.Api.Application.Services.Common;

namespace Back.Api.Application.Services;

public class SuperUsuarioService(ISuperUsuarioDomainRepository superUsuarioDomain, IPasswordService passwordService, IWebHostEnvironment hostEnvironment, ICommonService commonService) : ISuperUsuarioService
{
    #region Consultas colegios
    public async Task<ApplicationResult> GetColegiosAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await superUsuarioDomain.GetColegiosAsync(page, pageSize, cancellationToken));

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
    #endregion

    #region CRUD colegios
    public async Task<ApplicationResult> CreateColegioAsync(CreateColegioRequestDto createColegioRequestDto, CancellationToken cancellationToken = default)
    {
        var nombre = createColegioRequestDto.Nombre.Trim();
        var slug = NormalizeSlug(createColegioRequestDto.Slug);

        if (await superUsuarioDomain.ColegioSlugExistsAsync(slug, null, cancellationToken))
            return ApplicationResult.BadRequest("Ya existe un colegio con ese slug.");

        var created = await superUsuarioDomain.CreateColegioAsync(nombre, slug, createColegioRequestDto.LogoUrl, createColegioRequestDto.FaviconUrl, createColegioRequestDto.ColorPrimario, createColegioRequestDto.MensajeLogin, cancellationToken);
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
    #endregion

    #region Admins de colegio
    public async Task<ApplicationResult> CreateAdminColegioAsync(int colegioId, CreateAdminColegioRequestDto createAdminColegioRequestDto, CancellationToken cancellationToken = default)
    {
        var colegio = await superUsuarioDomain.GetColegioByIdAsync(colegioId, cancellationToken);
        if (colegio is null)
            return ApplicationResult.NotFound("Colegio no encontrado.");

        var slug = CredentialGenerationHelper.NormalizeSchoolSlugForDomain(colegio.Slug, colegio.Id);

        var password = CredentialGenerationHelper.GeneratePassword();

        var email = await commonService.GenerateUniqueEmailAsync(
            createAdminColegioRequestDto.Nombre,
            "admin",
            slug,
            e => superUsuarioDomain.ColegioCorreoDuplicadoAsync(colegioId, e, cancellationToken));

        var admin = await superUsuarioDomain.CreateAdminColegioAsync(
            colegioId,
            commonService.Normalize(createAdminColegioRequestDto.Nombre),
            email,
            passwordService.Hash(password),
            cancellationToken);

        admin.ContrasenaTemporal = password;

        return ApplicationResult.Created($"/api/superusuario/colegios/{colegioId}/admins/{admin.Id}", admin);
    }
    #endregion

    #region Imagenes de colegio
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
    #endregion

    #region Helpers
    private static string NormalizeSlug(string slug)
        => slug.Trim().ToLowerInvariant();
    #endregion
}
