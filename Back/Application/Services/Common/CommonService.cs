using Back.Api.Application.Configuration;
using Back.Api.Application.Common;
using System.Security.Claims;

namespace Back.Api.Application.Services.Common;

public class CommonService : ICommonService
{
    #region Consultas comunes
    public async Task<ApplicationResult> OkAsync<T>(Task<T> task)
        => ApplicationResult.Ok(await task);

    public ApplicationResult NotFoundIfNull<T>(T? value, string message)
        => value is null ? ApplicationResult.NotFound(message) : ApplicationResult.Ok(value);

    public string Normalize(string value)
        => value.Trim();

    public string NormalizeEmail(string email)
        => email.Trim().ToLowerInvariant();
    #endregion
    #region Validación de usuario
    public bool UsuarioCoincide(int entityId, ClaimsPrincipal user)
    {
        if (user.IsInRole(Roles.Admin)) return true;

        var idClaim = user.FindFirstValue("id") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(idClaim, out var id) && id == entityId;
    }
    #endregion
    #region Generación de credenciales
    public async Task<string> GenerateUniqueEmailAsync(string fullName, string rolePrefix, string schoolSlug, Func<string, Task<bool>> existsFunc)
    {
        for (int i = 0; i < 2000; i++)
        {
            var candidate = CredentialGenerationHelper.BuildGeneratedEmail(fullName, rolePrefix, schoolSlug, i);
            if (!await existsFunc(candidate))
                return candidate;
        }

        throw new InvalidOperationException("No se pudo generar el email.");
    }
    #endregion
    #region Manejo de archivos
    public async Task<string> SaveFileAsync(Stream fileStream, string originalName, string rootPath, string relativeFolder, CancellationToken cancellationToken = default)
    {
        var folder = Path.Combine(rootPath, relativeFolder);
        Directory.CreateDirectory(folder);

        var extension = Path.GetExtension(originalName);
        var file = Path.GetFileNameWithoutExtension(originalName);

        file = string.Concat(file.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));
        if (string.IsNullOrWhiteSpace(file)) file = "archivo";

        var newName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{file}{extension}";
        var full = Path.Combine(folder, newName);

        await using var fileStreamCreated = File.Create(full);
        await fileStream.CopyToAsync(fileStreamCreated, cancellationToken);

        return $"{relativeFolder}/{newName}".Replace("\\", "/");
    }
    #endregion
    #region Validaciones de archivos
    public ApplicationResult? ValidateFile(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return ApplicationResult.BadRequest("Debes adjuntar un archivo.");

        if (file.Length > 10 * 1024 * 1024)
            return ApplicationResult.BadRequest("El archivo supera el tamaño máximo permitido (10MB).");

        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".txt", ".jpg", ".jpeg", ".png"
        };

        var extension = Path.GetExtension(file.FileName);

        if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
            return ApplicationResult.BadRequest("Tipo de archivo no permitido.");

        return null;
    }
    #endregion
    #region Validaciones de colegio
    public ApplicationResult? Required(string value, string message)
        => string.IsNullOrWhiteSpace(value) ? ApplicationResult.BadRequest(message) : null;
    #endregion
}