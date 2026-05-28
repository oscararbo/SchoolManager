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

            await EnsureSubjectSchedulesAsync(db, cancellationToken);

            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (NpgsqlException ex)
        {
            logger.LogWarning(ex, "No se pudo conectar con PostgreSQL durante el seeding inicial.");
            return false;
        }
    }

    private static async Task EnsureSubjectSchedulesAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        const int minWeeklyBlocksPerSubject = 8;

        var subjects = await db.Asignaturas
            .AsNoTracking()
            .OrderBy(a => a.Id)
            .Select(a => new { a.Id, a.CursoId })
            .ToListAsync(cancellationToken);

        if (subjects.Count == 0)
            return;

        var existingScheduleRows = await db.HorariosAsignaturas
            .AsNoTracking()
            .Select(horario => new
            {
                horario.Id,
                horario.AsignaturaId,
                CursoId = horario.Asignatura != null ? horario.Asignatura.CursoId : 0,
                horario.DiaSemana,
                horario.HoraInicio,
                horario.HoraFin
            })
            .ToListAsync(cancellationToken);

        var conflictingScheduleIds = new HashSet<int>();
        foreach (var courseDayGroup in existingScheduleRows
            .GroupBy(row => new { row.CursoId, row.DiaSemana }))
        {
            var orderedRows = courseDayGroup
                .OrderBy(row => row.HoraInicio)
                .ThenBy(row => row.Id)
                .ToList();

            TimeOnly? lastEnd = null;
            foreach (var row in orderedRows)
            {
                if (lastEnd.HasValue && row.HoraInicio < lastEnd.Value)
                {
                    conflictingScheduleIds.Add(row.Id);
                    continue;
                }

                lastEnd = row.HoraFin;
            }
        }

        if (conflictingScheduleIds.Count > 0)
        {
            var rowsToSoftDelete = await db.HorariosAsignaturas
                .Where(horario => conflictingScheduleIds.Contains(horario.Id))
                .ToListAsync(cancellationToken);

            foreach (var row in rowsToSoftDelete)
                row.IsDeleted = true;

            existingScheduleRows = existingScheduleRows
                .Where(row => !conflictingScheduleIds.Contains(row.Id))
                .ToList();
        }

        var scheduleCountBySubject = existingScheduleRows
            .GroupBy(row => row.AsignaturaId)
            .ToDictionary(grouped => grouped.Key, grouped => grouped.Count());

        var occupiedSlotsBySubject = existingScheduleRows
            .GroupBy(row => row.AsignaturaId)
            .ToDictionary(
                grouped => grouped.Key,
                grouped => grouped
                    .Select(item => $"{item.DiaSemana}-{item.HoraInicio:HH\\:mm}")
                    .ToHashSet(StringComparer.Ordinal));

        var occupiedSlotsByCourse = existingScheduleRows
            .GroupBy(row => row.CursoId)
            .ToDictionary(
                grouped => grouped.Key,
                grouped => grouped
                    .Select(item => $"{item.DiaSemana}-{item.HoraInicio:HH\\:mm}")
                    .ToHashSet(StringComparer.Ordinal));

        var occupiedIntervalsByCourse = existingScheduleRows
            .GroupBy(row => row.CursoId)
            .ToDictionary(
                grouped => grouped.Key,
                grouped => grouped
                    .GroupBy(item => item.DiaSemana)
                    .ToDictionary(
                        dayGroup => dayGroup.Key,
                        dayGroup => dayGroup
                            .Select(item => (item.HoraInicio, item.HoraFin))
                            .ToList()));

        var dayStarts = new[]
        {
            new TimeOnly(8, 30),
            new TimeOnly(9, 25),
            new TimeOnly(10, 20),
            new TimeOnly(11, 15),
            new TimeOnly(12, 10),
            new TimeOnly(13, 5),
            new TimeOnly(14, 0),
            new TimeOnly(14, 55),
            new TimeOnly(15, 50),
            new TimeOnly(16, 45)
        };

        var baseTemplates = Enumerable.Range(1, 5)
            .SelectMany(day => dayStarts.Select(start => (DiaSemana: day, Inicio: start, Fin: start.Add(TimeSpan.FromMinutes(55)))))
            .ToArray();

        var aulaZones = new[] { "Norte", "Central", "Sur", "Laboratorio", "Tecnologia" };

        foreach (var subject in subjects)
        {
            if (!scheduleCountBySubject.TryGetValue(subject.Id, out var currentCount))
                currentCount = 0;

            if (!occupiedSlotsBySubject.TryGetValue(subject.Id, out var occupiedSlots))
            {
                occupiedSlots = new HashSet<string>(StringComparer.Ordinal);
                occupiedSlotsBySubject[subject.Id] = occupiedSlots;
            }

            if (!occupiedSlotsByCourse.TryGetValue(subject.CursoId, out var occupiedCourseSlots))
            {
                occupiedCourseSlots = new HashSet<string>(StringComparer.Ordinal);
                occupiedSlotsByCourse[subject.CursoId] = occupiedCourseSlots;
            }

            if (!occupiedIntervalsByCourse.TryGetValue(subject.CursoId, out var occupiedCourseIntervalsByDay))
            {
                occupiedCourseIntervalsByDay = new Dictionary<int, List<(TimeOnly HoraInicio, TimeOnly HoraFin)>>();
                occupiedIntervalsByCourse[subject.CursoId] = occupiedCourseIntervalsByDay;
            }

            if (currentCount >= minWeeklyBlocksPerSubject)
                continue;

            var baseIndex = (subject.Id + (subject.CursoId * 7)) % baseTemplates.Length;
            for (var i = 0; i < baseTemplates.Length && currentCount < minWeeklyBlocksPerSubject; i++)
            {
                var template = baseTemplates[(baseIndex + i) % baseTemplates.Length];
                var slotKey = $"{template.DiaSemana}-{template.Inicio:HH\\:mm}";

                if (!occupiedSlots.Add(slotKey))
                    continue;

                if (!occupiedCourseSlots.Add(slotKey))
                {
                    occupiedSlots.Remove(slotKey);
                    continue;
                }

                if (!occupiedCourseIntervalsByDay.TryGetValue(template.DiaSemana, out var occupiedIntervalsInDay))
                {
                    occupiedIntervalsInDay = new List<(TimeOnly HoraInicio, TimeOnly HoraFin)>();
                    occupiedCourseIntervalsByDay[template.DiaSemana] = occupiedIntervalsInDay;
                }

                var overlapsInCourse = occupiedIntervalsInDay.Any(interval =>
                    template.Inicio < interval.HoraFin
                    && interval.HoraInicio < template.Fin);

                if (overlapsInCourse)
                {
                    occupiedCourseSlots.Remove(slotKey);
                    occupiedSlots.Remove(slotKey);
                    continue;
                }

                db.HorariosAsignaturas.Add(new HorarioAsignatura
                {
                    AsignaturaId = subject.Id,
                    DiaSemana = template.DiaSemana,
                    HoraInicio = template.Inicio,
                    HoraFin = template.Fin,
                    Aula = $"{aulaZones[subject.CursoId % aulaZones.Length]}-{template.DiaSemana}{10 + ((subject.Id + currentCount) % 20)}"
                });

                occupiedIntervalsInDay.Add((template.Inicio, template.Fin));

                currentCount++;
            }
        }
    }
}