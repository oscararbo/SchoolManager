using System.Security.Claims;
using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Abstractions.Security;
using Back.Api.Application.Configuration;

namespace Back.Api.Infrastructure.Security;

/// <summary>
/// Implementación del servicio de validación de usuario-colegio.
/// Verifica en cada request que el usuario autenticado pertenece al colegio indicado en headers.
/// </summary>
public sealed class UserSchoolValidationService(IAuthDomainRepository authRepository) : IUserSchoolValidationService
{
    private const int SuperUsuarioSchoolBypassId = -1;

    public async Task<int> ValidateUserBelongsToSchoolAsync(ClaimsPrincipal user, string? schoolSlug, CancellationToken cancellationToken = default)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("Usuario no autenticado.");
        }

        #region Superusuario bypass
        var roleClaim = user.FindFirstValue(ClaimTypes.Role);
        if (string.Equals(roleClaim, Roles.SuperUsuario, StringComparison.OrdinalIgnoreCase))
        {
            return SuperUsuarioSchoolBypassId;
        }
        #endregion

        if (string.IsNullOrWhiteSpace(schoolSlug))
        {
            throw new InvalidOperationException("El colegio no fue especificado en la solicitud.");
        }

        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            throw new InvalidOperationException("No se pudo obtener el ID del usuario.");
        }

        #region School lookup
        var colegio = await authRepository.GetColegioBySlugAsync(schoolSlug, cancellationToken);
        if (colegio == null)
        {
            throw new KeyNotFoundException($"El colegio '{schoolSlug}' no existe.");
        }
        #endregion

        #region School membership validation
        var userBelongsToSchool = await authRepository.UserBelongsToSchoolAsync(userId, colegio.Id, cancellationToken);
        if (!userBelongsToSchool)
        {
            throw new UnauthorizedAccessException("No tienes acceso a este colegio.");
        }
        #endregion

        return colegio.Id;
    }
}
