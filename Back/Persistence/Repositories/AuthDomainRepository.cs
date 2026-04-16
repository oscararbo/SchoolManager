using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Configuration;
using Back.Api.Persistence.Context;
using Back.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Back.Api.Persistence.Repositories;

public class AuthDomainRepository(AppDbContext context) : IAuthDomainRepository
{
    public Task<Admin?> FindAdminByCorreoAsync(string correo, CancellationToken cancellationToken = default)
        => context.Admins
            .Include(a => a.Cuenta)
            .FirstOrDefaultAsync(a => a.Cuenta != null && a.Cuenta.Correo == correo, cancellationToken);

    public Task<Profesor?> FindProfesorByCorreoAsync(string correo, CancellationToken cancellationToken = default)
        => context.Profesores
            .Include(p => p.Cuenta)
            .FirstOrDefaultAsync(p => p.Cuenta != null && p.Cuenta.Correo == correo, cancellationToken);

    public Task<Estudiante?> FindEstudianteByCorreoAsync(string correo, CancellationToken cancellationToken = default)
        => context.Estudiantes
            .Include(e => e.Curso)
            .Include(e => e.Cuenta)
            .FirstOrDefaultAsync(e => e.Cuenta != null && e.Cuenta.Correo == correo, cancellationToken);

    public Task<RefreshToken?> FindRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
        => context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token, cancellationToken);

    public async Task RevokeTokenAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        token.RevokedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> CreateRefreshTokenAsync(int userId, string rol, int expireDays, CancellationToken cancellationToken = default)
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

        await context.SaveChangesAsync(cancellationToken);
        return tokenValue;
    }

    public async Task<string?> ObtenerCorreoAsync(int userId, string rol, CancellationToken cancellationToken = default)
    {
        if (rol == Roles.Admin)
            return await context.Admins
                .Where(a => a.Id == userId)
                .Select(a => (string?)a.Cuenta!.Correo)
                .FirstOrDefaultAsync(cancellationToken);

        if (rol == Roles.Profesor)
            return await context.Profesores
                .Where(p => p.Id == userId)
                .Select(p => (string?)p.Cuenta!.Correo)
                .FirstOrDefaultAsync(cancellationToken);

        if (rol == Roles.Alumno)
            return await context.Estudiantes
                .Where(e => e.Id == userId)
                .Select(e => (string?)e.Cuenta!.Correo)
                .FirstOrDefaultAsync(cancellationToken);

        return null;
    }
}
