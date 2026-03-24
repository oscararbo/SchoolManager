using Back.Api.Domain.Entities;

namespace Back.Api.Application.Abstractions.Repositories;

public interface IAuthDomainRepository
{
    Task<Admin?> FindAdminByCorreoAsync(string correo, CancellationToken cancellationToken = default);
    Task<Profesor?> FindProfesorByCorreoAsync(string correo, CancellationToken cancellationToken = default);
    Task<Estudiante?> FindEstudianteByCorreoAsync(string correo, CancellationToken cancellationToken = default);
    Task<RefreshToken?> FindRefreshTokenAsync(string token, CancellationToken cancellationToken = default);
    Task RevokeTokenAsync(RefreshToken token, CancellationToken cancellationToken = default);
    Task<string> CreateRefreshTokenAsync(int userId, string rol, int expireDays, CancellationToken cancellationToken = default);
    Task<string?> ObtenerCorreoAsync(int userId, string rol, CancellationToken cancellationToken = default);
}
