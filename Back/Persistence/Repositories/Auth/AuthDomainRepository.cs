using Back.Api.Persistence.Context;
using Back.Api.Domain.Entities;
using Back.Api.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Back.Api.Persistence.Repositories;

public class AuthDomainRepository(AppDbContext context) : IAuthDomainRepository
{
    public Task<Admin?> FindAdminByCorreoAsync(string correo)
        => context.Admins.FirstOrDefaultAsync(a => a.Correo == correo);

    public Task<Profesor?> FindProfesorByCorreoAsync(string correo)
        => context.Profesores.FirstOrDefaultAsync(p => p.Correo == correo);

    public Task<Estudiante?> FindEstudianteByCorreoAsync(string correo)
        => context.Estudiantes.Include(e => e.Curso).FirstOrDefaultAsync(e => e.Correo == correo);

    public Task<RefreshToken?> FindRefreshTokenAsync(string token)
        => context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token);

    public async Task RevokeTokenAsync(RefreshToken token)
    {
        token.RevokedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task<string> CreateRefreshTokenAsync(int userId, string rol, int expireDays)
    {
        var tokenValue = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                       + Convert.ToBase64String(Guid.NewGuid().ToByteArray());

        context.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            Rol = rol,
            Token = tokenValue,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(expireDays)
        });

        await context.SaveChangesAsync();
        return tokenValue;
    }

    public async Task<string?> ObtenerCorreoAsync(int userId, string rol)
    {
        if (rol == "admin")
            return await context.Admins
                .Where(a => a.Id == userId)
                .Select(a => (string?)a.Correo)
                .FirstOrDefaultAsync();

        if (rol == "profesor")
            return await context.Profesores
                .Where(p => p.Id == userId)
                .Select(p => (string?)p.Correo)
                .FirstOrDefaultAsync();

        if (rol == "alumno")
            return await context.Estudiantes
                .Where(e => e.Id == userId)
                .Select(e => (string?)e.Correo)
                .FirstOrDefaultAsync();

        return null;
    }
}
