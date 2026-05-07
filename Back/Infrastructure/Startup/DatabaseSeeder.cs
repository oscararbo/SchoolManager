using Back.Api.Application.Abstractions.Security;
using Back.Api.Application.Configuration;
using Back.Api.Domain.Entities;
using Back.Api.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Back.Api.Infrastructure.Startup;

public sealed class DatabaseSeeder
{
    private readonly IServiceProvider serviceProvider;
    private readonly IConfiguration configuration;
    private readonly ILogger<DatabaseSeeder> logger;

    public DatabaseSeeder(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<DatabaseSeeder> logger)
    {
        this.serviceProvider = serviceProvider;
        this.configuration = configuration;
        this.logger = logger;
    }

    public async Task<bool> SeedAsync(CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();

        try
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

            var seedAdminName = configuration["SeedAdmin:Nombre"] ?? "Administrador";
            var seedAdminEmail = (configuration["SeedAdmin:Correo"] ?? "admin@prueba.com").Trim().ToLowerInvariant();
            var seedAdminPassword = configuration["SeedAdmin:Contrasena"] ?? "Prueba1";
            var seedColegioNombre = configuration["SeedSchool:Nombre"] ?? "Colegio Principal";
            var seedColegioSlug = (configuration["SeedSchool:Slug"] ?? "default").Trim().ToLowerInvariant();
            var seedColegioLogoUrl = configuration["SeedSchool:LogoUrl"];
            var seedColegioFaviconUrl = configuration["SeedSchool:FaviconUrl"];
            var seedColegioColorPrimario = configuration["SeedSchool:ColorPrimario"] ?? "#1f2937";
            var seedColegioMensajeLogin = configuration["SeedSchool:MensajeLogin"] ?? "Consulta tus clases, tus asignaturas y tus notas en un solo lugar.";
            var seedSuperUsuarioEmail = (configuration["SeedSuperUsuario:Correo"] ?? "root@schoolmanager.com").Trim().ToLowerInvariant();
            var seedSuperUsuarioPassword = configuration["SeedSuperUsuario:Contrasena"] ?? "Super123!";

            if (db.Database.IsRelational())
            {
                if (db.Database.GetMigrations().Any())
                    await db.Database.MigrateAsync(cancellationToken);
                else
                    await db.Database.EnsureCreatedAsync(cancellationToken);
            }
            else
            {
                await db.Database.EnsureCreatedAsync(cancellationToken);
            }

            var colegio = await db.Colegios.FirstOrDefaultAsync(c => c.Slug == seedColegioSlug, cancellationToken);
            if (colegio is null)
            {
                colegio = new Colegio
                {
                    Nombre = seedColegioNombre,
                    Slug = seedColegioSlug,
                    LogoUrl = string.IsNullOrWhiteSpace(seedColegioLogoUrl) ? null : seedColegioLogoUrl,
                    FaviconUrl = string.IsNullOrWhiteSpace(seedColegioFaviconUrl) ? null : seedColegioFaviconUrl,
                    ColorPrimario = string.IsNullOrWhiteSpace(seedColegioColorPrimario) ? null : seedColegioColorPrimario,
                    MensajeLogin = string.IsNullOrWhiteSpace(seedColegioMensajeLogin) ? null : seedColegioMensajeLogin
                };
                db.Colegios.Add(colegio);
                await db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                colegio.Nombre = seedColegioNombre;
                colegio.LogoUrl = string.IsNullOrWhiteSpace(seedColegioLogoUrl) ? null : seedColegioLogoUrl;
                colegio.FaviconUrl = string.IsNullOrWhiteSpace(seedColegioFaviconUrl) ? null : seedColegioFaviconUrl;
                colegio.ColorPrimario = string.IsNullOrWhiteSpace(seedColegioColorPrimario) ? null : seedColegioColorPrimario;
                colegio.MensajeLogin = string.IsNullOrWhiteSpace(seedColegioMensajeLogin) ? null : seedColegioMensajeLogin;
                await db.SaveChangesAsync(cancellationToken);
            }

            var adminExistente = await db.Admins
                .Include(a => a.Cuenta)
                .FirstOrDefaultAsync(a => a.Cuenta != null && a.Cuenta.Correo == seedAdminEmail, cancellationToken);

            if (adminExistente is null)
            {
                db.Admins.Add(new()
                {
                    Nombre = seedAdminName,
                    Cuenta = new Cuenta
                    {
                        Correo = seedAdminEmail,
                        Contrasena = passwordService.Hash(seedAdminPassword),
                        Rol = Roles.Admin,
                        ColegioId = colegio.Id
                    }
                });
            }
            else
            {
                adminExistente.Nombre = seedAdminName;
                if (adminExistente.Cuenta is not null)
                {
                    adminExistente.Cuenta.Correo = seedAdminEmail;
                    adminExistente.Cuenta.Contrasena = passwordService.Hash(seedAdminPassword);
                    adminExistente.Cuenta.Rol = Roles.Admin;
                    adminExistente.Cuenta.ColegioId = colegio.Id;
                }
            }

            var superUsuarioExistente = await db.Cuentas
                .FirstOrDefaultAsync(c => c.Correo == seedSuperUsuarioEmail && c.Rol == Roles.SuperUsuario, cancellationToken);

            if (superUsuarioExistente is null)
            {
                db.Cuentas.Add(new Cuenta
                {
                    Correo = seedSuperUsuarioEmail,
                    Contrasena = passwordService.Hash(seedSuperUsuarioPassword),
                    Rol = Roles.SuperUsuario,
                    ColegioId = null
                });
            }
            else
            {
                superUsuarioExistente.Contrasena = passwordService.Hash(seedSuperUsuarioPassword);
                superUsuarioExistente.Rol = Roles.SuperUsuario;
                superUsuarioExistente.ColegioId = null;
            }

            var cuentasSinColegio = await db.Cuentas
                .Where(c => c.ColegioId == null && c.Rol != Roles.SuperUsuario)
                .ToListAsync(cancellationToken);

            foreach (var cuenta in cuentasSinColegio)
            {
                cuenta.ColegioId = colegio.Id;
            }

            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (NpgsqlException ex)
        {
            logger.LogWarning(ex, "No se pudo conectar con PostgreSQL durante el seeding inicial.");
            return false;
        }
    }
}