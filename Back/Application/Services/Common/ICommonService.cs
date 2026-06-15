using Back.Api.Application.Common;
using System.Security.Claims;

namespace Back.Api.Application.Services.Common;

public interface ICommonService
{
    Task<ApplicationResult> OkAsync<T>(Task<T> task);
    ApplicationResult NotFoundIfNull<T>(T? value, string message);
    ApplicationResult? Required(string value, string message);
    string Normalize(string value);
    string NormalizeEmail(string email);
    bool UsuarioCoincide(int entityId, ClaimsPrincipal user);
    Task<string> GenerateUniqueEmailAsync(string fullName, string rolePrefix, string schoolSlug, Func<string, Task<bool>> existsFunc);
    ApplicationResult? ValidateFile(IFormFile? file);
    Task<string> SaveFileAsync(Stream fileStream, string originalName, string rootPath, string relativeFolder, CancellationToken cancellationToken);
}
