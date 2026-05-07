using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Configuration;
using Back.Api.Persistence.Context;
using Back.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Back.Api.Persistence.Repositories;

public class AuthDomainRepository(AppDbContext context) : IAuthDomainRepository
{
    public Task<Admin?> FindAdminByCorreoAsync(string correo, string colegioSlug, CancellationToken cancellationToken = default)
        => context.Admins
            .Include(a => a.Cuenta)
            .ThenInclude(c => c!.Colegio)
            .FirstOrDefaultAsync(a => a.Cuenta != null &&
                a.Cuenta.Correo == correo &&
                a.Cuenta.Colegio != null &&
                a.Cuenta.Colegio.Slug == colegioSlug,
                cancellationToken);

    public Task<Profesor?> FindProfesorByCorreoAsync(string correo, string colegioSlug, CancellationToken cancellationToken = default)
        => context.Profesores
            .Include(p => p.Cuenta)
            .ThenInclude(c => c!.Colegio)
            .FirstOrDefaultAsync(p => p.Cuenta != null &&
                p.Cuenta.Correo == correo &&
                p.Cuenta.Colegio != null &&
                p.Cuenta.Colegio.Slug == colegioSlug,
                cancellationToken);

    public Task<Estudiante?> FindEstudianteByCorreoAsync(string correo, string colegioSlug, CancellationToken cancellationToken = default)
        => context.Estudiantes
            .Include(e => e.Curso)
            .Include(e => e.Cuenta)
            .ThenInclude(c => c!.Colegio)
            .FirstOrDefaultAsync(e => e.Cuenta != null &&
                e.Cuenta.Correo == correo &&
                e.Cuenta.Colegio != null &&
                e.Cuenta.Colegio.Slug == colegioSlug,
                cancellationToken);

    public Task<Cuenta?> FindSuperUsuarioByCorreoAsync(string correo, CancellationToken cancellationToken = default)
        => context.Cuentas
            .FirstOrDefaultAsync(c => c.Correo == correo && c.Rol == Roles.SuperUsuario, cancellationToken);

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

    public async Task<AuthUserContext?> GetUserContextAsync(int userId, string rol, CancellationToken cancellationToken = default)
    {
        if (rol == Roles.SuperUsuario)
        {
            return await context.Cuentas
                .Where(c => c.Id == userId && c.Rol == Roles.SuperUsuario)
                .Select(c => new AuthUserContext
                {
                    UserId = c.Id,
                    Correo = c.Correo,
                    Rol = c.Rol
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (rol == Roles.Admin)
            return await context.Admins
                .Where(a => a.Id == userId)
                .Select(a => new AuthUserContext
                {
                    UserId = a.Id,
                    Correo = a.Cuenta!.Correo,
                    Rol = a.Cuenta.Rol,
                    ColegioId = a.Cuenta.ColegioId,
                    ColegioNombre = a.Cuenta.Colegio != null ? a.Cuenta.Colegio.Nombre : null,
                    ColegioSlug = a.Cuenta.Colegio != null ? a.Cuenta.Colegio.Slug : null,
                    ColegioLogoUrl = a.Cuenta.Colegio != null ? a.Cuenta.Colegio.LogoUrl : null
                })
                .FirstOrDefaultAsync(cancellationToken);

        if (rol == Roles.Profesor)
            return await context.Profesores
                .Where(p => p.Id == userId)
                .Select(p => new AuthUserContext
                {
                    UserId = p.Id,
                    Correo = p.Cuenta!.Correo,
                    Rol = p.Cuenta.Rol,
                    ColegioId = p.Cuenta.ColegioId,
                    ColegioNombre = p.Cuenta.Colegio != null ? p.Cuenta.Colegio.Nombre : null,
                    ColegioSlug = p.Cuenta.Colegio != null ? p.Cuenta.Colegio.Slug : null,
                    ColegioLogoUrl = p.Cuenta.Colegio != null ? p.Cuenta.Colegio.LogoUrl : null
                })
                .FirstOrDefaultAsync(cancellationToken);

        if (rol == Roles.Alumno)
            return await context.Estudiantes
                .Where(e => e.Id == userId)
                .Select(e => new AuthUserContext
                {
                    UserId = e.Id,
                    Correo = e.Cuenta!.Correo,
                    Rol = e.Cuenta.Rol,
                    ColegioId = e.Cuenta.ColegioId,
                    ColegioNombre = e.Cuenta.Colegio != null ? e.Cuenta.Colegio.Nombre : null,
                    ColegioSlug = e.Cuenta.Colegio != null ? e.Cuenta.Colegio.Slug : null,
                    ColegioLogoUrl = e.Cuenta.Colegio != null ? e.Cuenta.Colegio.LogoUrl : null
                })
                .FirstOrDefaultAsync(cancellationToken);

        return null;
    }
}
