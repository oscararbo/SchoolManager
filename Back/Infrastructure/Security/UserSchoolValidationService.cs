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
    public async Task<int> ValidateUserBelongsToSchoolAsync(ClaimsPrincipal user, string? schoolSlug, CancellationToken cancellationToken = default)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("Usuario no autenticado.");
        }

        // Los superusuarios no necesitan validación de colegio
        var roleClaim = user.FindFirstValue(ClaimTypes.Role);
        if (string.Equals(roleClaim, Roles.SuperUsuario, StringComparison.OrdinalIgnoreCase))
        {
            return -1; // Indicador de superusuario sin colegio específico
        }

        if (string.IsNullOrWhiteSpace(schoolSlug))
        {
            throw new InvalidOperationException("El colegio no fue especificado en la solicitud.");
        }

        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            throw new InvalidOperationException("No se pudo obtener el ID del usuario.");
        }

        // Obtener el colegio por slug
        var colegio = await authRepository.GetColegioBySlugAsync(schoolSlug, cancellationToken);
        if (colegio == null)
        {
            throw new KeyNotFoundException($"El colegio '{schoolSlug}' no existe.");
        }

        // Validar que el usuario pertenece a este colegio
        var userBelongsToSchool = await authRepository.UserBelongsToSchoolAsync(userId, colegio.Id, cancellationToken);
        if (!userBelongsToSchool)
        {
            throw new UnauthorizedAccessException("No tienes acceso a este colegio.");
        }

        return colegio.Id;
    }
}
