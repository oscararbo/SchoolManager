using System.Security.Claims;

namespace Back.Api.Application.Abstractions.Security;

/// <summary>
/// Servicio que valida que un usuario pertenece a un colegio específico.
/// Esto se ejecuta en cada request para prevenir acceso no autorizado a recursos de otros colegios.
/// </summary>
public interface IUserSchoolValidationService
{
    /// <summary>
    /// Valida que el usuario actual pertenece al colegio indicado en los headers.
    /// Lanza una excepción si el usuario no pertenece al colegio o si no hay información de colegio.
    /// </summary>
    /// <param name="user">El ClaimsPrincipal del usuario actual</param>
    /// <param name="schoolSlug">El slug del colegio del request header</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>El ID del colegio validado</returns>
    Task<int> ValidateUserBelongsToSchoolAsync(ClaimsPrincipal user, string? schoolSlug, CancellationToken cancellationToken = default);
}
