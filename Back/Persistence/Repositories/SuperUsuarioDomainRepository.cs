using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Dtos;
using Back.Api.Application.Configuration;
using Back.Api.Domain.Entities;
using Back.Api.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Back.Api.Persistence.Repositories;

public class SuperUsuarioDomainRepository(AppDbContext context) : ISuperUsuarioDomainRepository
{
    public async Task<IEnumerable<ColegioListItemDto>> GetColegiosAsync(CancellationToken cancellationToken = default)
        => await context.Colegios
            .AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new ColegioListItemDto
            {
                Id = c.Id,
                Nombre = c.Nombre,
                Slug = c.Slug,
                LogoUrl = c.LogoUrl,
                FaviconUrl = c.FaviconUrl,
                ColorPrimario = c.ColorPrimario,
                MensajeLogin = c.MensajeLogin,
                TotalAdmins = c.Cuentas.Count(cuenta => cuenta.Rol == Roles.Admin && !cuenta.IsDeleted),
                TotalProfesores = c.Cuentas.Count(cuenta => cuenta.Rol == Roles.Profesor && !cuenta.IsDeleted),
                TotalAlumnos = c.Cuentas.Count(cuenta => cuenta.Rol == Roles.Alumno && !cuenta.IsDeleted),
                TotalCursos = c.Cursos.Count(curso => !curso.IsDeleted)
            })
            .ToListAsync(cancellationToken);

    public Task<ColegioListItemDto?> GetColegioBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => context.Colegios
            .AsNoTracking()
            .Where(c => c.Slug == slug)
            .Select(c => new ColegioListItemDto
            {
                Id = c.Id,
                Nombre = c.Nombre,
                Slug = c.Slug,
                LogoUrl = c.LogoUrl,
                FaviconUrl = c.FaviconUrl,
                ColorPrimario = c.ColorPrimario,
                MensajeLogin = c.MensajeLogin,
                TotalAdmins = c.Cuentas.Count(cuenta => cuenta.Rol == Roles.Admin && !cuenta.IsDeleted),
                TotalProfesores = c.Cuentas.Count(cuenta => cuenta.Rol == Roles.Profesor && !cuenta.IsDeleted),
                TotalAlumnos = c.Cuentas.Count(cuenta => cuenta.Rol == Roles.Alumno && !cuenta.IsDeleted),
                TotalCursos = c.Cursos.Count(curso => !curso.IsDeleted)
            })
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IEnumerable<ColegioAdminListItemDto>> GetAdminsByColegioAsync(int colegioId, CancellationToken cancellationToken = default)
        => await context.Admins
            .AsNoTracking()
            .Where(admin => admin.Cuenta != null && admin.Cuenta.ColegioId == colegioId)
            .OrderBy(admin => admin.Nombre)
            .Select(admin => new ColegioAdminListItemDto
            {
                Id = admin.Id,
                Nombre = admin.Nombre,
                Correo = admin.Cuenta!.Correo,
                ColegioId = colegioId,
                Colegio = admin.Cuenta.Colegio != null ? admin.Cuenta.Colegio.Nombre : string.Empty
            })
            .ToListAsync(cancellationToken);

    public Task<bool> ColegioSlugExistsAsync(string slug, int? exceptColegioId = null, CancellationToken cancellationToken = default)
        => context.Colegios.AnyAsync(c => c.Slug == slug && (!exceptColegioId.HasValue || c.Id != exceptColegioId.Value), cancellationToken);

    public async Task<ColegioListItemDto> CreateColegioAsync(string nombre, string slug, string? logoUrl, string? faviconUrl, string? colorPrimario, string? mensajeLogin, CancellationToken cancellationToken = default)
    {
        var colegio = new Colegio
        {
            Nombre = nombre,
            Slug = slug,
            LogoUrl = NormalizeOptional(logoUrl),
            FaviconUrl = NormalizeOptional(faviconUrl),
            ColorPrimario = NormalizeOptional(colorPrimario),
            MensajeLogin = NormalizeOptional(mensajeLogin)
        };

        context.Colegios.Add(colegio);
        await context.SaveChangesAsync(cancellationToken);

        return new ColegioListItemDto
        {
            Id = colegio.Id,
            Nombre = colegio.Nombre,
            Slug = colegio.Slug,
            LogoUrl = colegio.LogoUrl,
            FaviconUrl = colegio.FaviconUrl,
            ColorPrimario = colegio.ColorPrimario,
            MensajeLogin = colegio.MensajeLogin,
            TotalCursos = 0,
            TotalAdmins = 0,
            TotalProfesores = 0,
            TotalAlumnos = 0
        };
    }

    public async Task<ColegioListItemDto?> UpdateColegioAsync(int colegioId, string nombre, string slug, string? logoUrl, string? faviconUrl, string? colorPrimario, string? mensajeLogin, CancellationToken cancellationToken = default)
    {
        var colegio = await context.Colegios.FirstOrDefaultAsync(c => c.Id == colegioId, cancellationToken);
        if (colegio is null)
            return null;

        colegio.Nombre = nombre;
        colegio.Slug = slug;
        colegio.LogoUrl = NormalizeOptional(logoUrl);
        colegio.FaviconUrl = NormalizeOptional(faviconUrl);
        colegio.ColorPrimario = NormalizeOptional(colorPrimario);
        colegio.MensajeLogin = NormalizeOptional(mensajeLogin);

        await context.SaveChangesAsync(cancellationToken);

        return new ColegioListItemDto
        {
            Id = colegio.Id,
            Nombre = colegio.Nombre,
            Slug = colegio.Slug,
            LogoUrl = colegio.LogoUrl,
            FaviconUrl = colegio.FaviconUrl,
            ColorPrimario = colegio.ColorPrimario,
            MensajeLogin = colegio.MensajeLogin,
            TotalAdmins = await context.Cuentas.CountAsync(cuenta => cuenta.ColegioId == colegioId && cuenta.Rol == Roles.Admin, cancellationToken),
            TotalProfesores = await context.Cuentas.CountAsync(cuenta => cuenta.ColegioId == colegioId && cuenta.Rol == Roles.Profesor, cancellationToken),
            TotalAlumnos = await context.Cuentas.CountAsync(cuenta => cuenta.ColegioId == colegioId && cuenta.Rol == Roles.Alumno, cancellationToken),
            TotalCursos = await context.Cursos.CountAsync(curso => curso.ColegioId == colegioId, cancellationToken)
        };
    }

    public async Task<bool> DeleteColegioAsync(int colegioId, CancellationToken cancellationToken = default)
    {
        var colegio = await context.Colegios.FirstOrDefaultAsync(c => c.Id == colegioId, cancellationToken);
        if (colegio is null)
            return false;

        context.Colegios.Remove(colegio);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<bool> ColegioCorreoDuplicadoAsync(int colegioId, string correo, CancellationToken cancellationToken = default)
        => context.Cuentas.AnyAsync(c => c.ColegioId == colegioId && c.Correo == correo, cancellationToken);

    public async Task<ColegioAdminListItemDto> CreateAdminColegioAsync(int colegioId, string nombre, string correo, string contrasenaHash, CancellationToken cancellationToken = default)
    {
        var admin = new Admin
        {
            Nombre = nombre,
            Cuenta = new Cuenta
            {
                Correo = correo,
                Contrasena = contrasenaHash,
                Rol = Roles.Admin,
                ColegioId = colegioId
            }
        };

        context.Admins.Add(admin);
        await context.SaveChangesAsync(cancellationToken);

        return new ColegioAdminListItemDto
        {
            Id = admin.Id,
            Nombre = admin.Nombre,
            Correo = admin.Cuenta?.Correo ?? correo,
            ColegioId = colegioId,
            Colegio = await context.Colegios.Where(c => c.Id == colegioId).Select(c => c.Nombre).FirstAsync(cancellationToken)
        };
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
