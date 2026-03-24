using Back.Api.Domain.Entities;

namespace Back.Api.Domain.Repositories;

public interface IAuthDomainRepository
{
    Task<Admin?> FindAdminByCorreoAsync(string correo);
    Task<Profesor?> FindProfesorByCorreoAsync(string correo);
    Task<Estudiante?> FindEstudianteByCorreoAsync(string correo);
    Task<RefreshToken?> FindRefreshTokenAsync(string token);
    Task RevokeTokenAsync(RefreshToken token);
    Task<string> CreateRefreshTokenAsync(int userId, string rol, int expireDays);
    Task<string?> ObtenerCorreoAsync(int userId, string rol);
}
