using Back.Api.Application.Abstractions.Security;
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

            var adminExistente = await db.Admins.FirstOrDefaultAsync(a => a.Correo == seedAdminEmail, cancellationToken);
            if (adminExistente is null)
            {
                db.Admins.Add(new()
                {
                    Nombre = seedAdminName,
                    Correo = seedAdminEmail,
                    Contrasena = passwordService.Hash(seedAdminPassword)
                });
            }
            else
            {
                adminExistente.Nombre = seedAdminName;
                adminExistente.Correo = seedAdminEmail;
                adminExistente.Contrasena = passwordService.Hash(seedAdminPassword);
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