using Back.Api.Domain.Entities;

namespace Back.Api.Application.Abstractions.Repositories;

public interface IAuthDomainRepository
{
    Task<Admin?> FindAdminByCorreoAsync(string correo, string colegioSlug, CancellationToken cancellationToken = default);
    Task<Profesor?> FindProfesorByCorreoAsync(string correo, string colegioSlug, CancellationToken cancellationToken = default);
    Task<Estudiante?> FindEstudianteByCorreoAsync(string correo, string colegioSlug, CancellationToken cancellationToken = default);
    Task<Cuenta?> FindSuperUsuarioByCorreoAsync(string correo, CancellationToken cancellationToken = default);
    Task<RefreshToken?> FindRefreshTokenAsync(string token, CancellationToken cancellationToken = default);
    Task RevokeTokenAsync(RefreshToken token, CancellationToken cancellationToken = default);
    Task<string> CreateRefreshTokenAsync(int userId, string rol, int expireDays, CancellationToken cancellationToken = default);
    Task<AuthUserContext?> GetUserContextAsync(int userId, string rol, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Obtiene un colegio por su slug para validación de colegio en requests.
    /// </summary>
    Task<Colegio?> GetColegioBySlugAsync(string slug, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Valida que un usuario (por ID) pertenece a un colegio específico (por ID de colegio).
    /// </summary>
    Task<bool> UserBelongsToSchoolAsync(int userId, int schoolId, CancellationToken cancellationToken = default);
}
