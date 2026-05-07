using Back.Api.Domain.Entities;
using Back.Api.Application.Abstractions.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Back.Api.Persistence.Context;

public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentSchoolContext currentSchoolContext) : DbContext(options)
{
    public DbSet<Colegio> Colegios => Set<Colegio>();
    public DbSet<Cuenta> Cuentas => Set<Cuenta>();
    public DbSet<Curso> Cursos => Set<Curso>();
    public DbSet<Estudiante> Estudiantes => Set<Estudiante>();
    public DbSet<Profesor> Profesores => Set<Profesor>();
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<Asignatura> Asignaturas => Set<Asignatura>();
    public DbSet<EstudianteAsignatura> EstudianteAsignaturas => Set<EstudianteAsignatura>();
    public DbSet<ProfesorAsignaturaCurso> ProfesorAsignaturaCursos => Set<ProfesorAsignaturaCurso>();
    public DbSet<Nota> Notas => Set<Nota>();
    public DbSet<Tarea> Tareas => Set<Tarea>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    private int? CurrentSchoolId => currentSchoolContext.IsSuperUsuario ? null : currentSchoolContext.SchoolId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureSoftDelete<Colegio>(modelBuilder);
        ConfigureSoftDelete<Cuenta>(modelBuilder);
        ConfigureSoftDelete<Curso>(modelBuilder);
        ConfigureSoftDelete<Estudiante>(modelBuilder);
        ConfigureSoftDelete<Profesor>(modelBuilder);
        ConfigureSoftDelete<Admin>(modelBuilder);
        ConfigureSoftDelete<Asignatura>(modelBuilder);
        ConfigureSoftDelete<EstudianteAsignatura>(modelBuilder);
        ConfigureSoftDelete<ProfesorAsignaturaCurso>(modelBuilder);
        ConfigureSoftDelete<Nota>(modelBuilder);
        ConfigureSoftDelete<Tarea>(modelBuilder);
        ConfigureSoftDelete<RefreshToken>(modelBuilder);

        modelBuilder.Entity<Cuenta>()
            .HasIndex(c => new { c.ColegioId, c.Correo })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        modelBuilder.Entity<Colegio>()
            .HasIndex(c => c.Slug)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        modelBuilder.Entity<Cuenta>()
            .HasOne(c => c.Colegio)
            .WithMany(colegio => colegio.Cuentas)
            .HasForeignKey(c => c.ColegioId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Curso>()
            .HasOne(c => c.Colegio)
            .WithMany(colegio => colegio.Cursos)
            .HasForeignKey(c => c.ColegioId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Admin>()
            .HasOne(a => a.Cuenta)
            .WithOne(c => c.Admin)
            .HasForeignKey<Admin>(a => a.CuentaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Profesor>()
            .HasOne(p => p.Cuenta)
            .WithOne(c => c.Profesor)
            .HasForeignKey<Profesor>(p => p.CuentaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Estudiante>()
            .HasOne(e => e.Cuenta)
            .WithOne(c => c.Estudiante)
            .HasForeignKey<Estudiante>(e => e.CuentaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Asignatura>()
            .HasOne(a => a.Curso)
            .WithMany(c => c.Asignaturas)
            .HasForeignKey(a => a.CursoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Tarea>()
            .HasOne(t => t.Profesor)
            .WithMany(p => p.Tareas)
            .HasForeignKey(t => t.ProfesorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Asignatura>()
            .HasIndex(a => new { a.CursoId, a.Nombre })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        modelBuilder.Entity<EstudianteAsignatura>()
            .HasKey(x => new { x.EstudianteId, x.AsignaturaId });

        modelBuilder.Entity<ProfesorAsignaturaCurso>()
            .HasKey(x => new { x.ProfesorId, x.AsignaturaId, x.CursoId });

        modelBuilder.Entity<Nota>()
            .Property(x => x.Valor)
            .HasPrecision(4, 2);

        modelBuilder.Entity<Nota>()
            .HasIndex(x => new { x.EstudianteId, x.TareaId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(x => x.Token)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(x => new { x.UserId, x.Rol });

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(x => x.ExpiresAtUtc);

        modelBuilder.Entity<Colegio>()
            .HasQueryFilter(c => !c.IsDeleted);

        modelBuilder.Entity<Cuenta>()
            .HasQueryFilter(c => !c.IsDeleted && (CurrentSchoolId == null || c.ColegioId == CurrentSchoolId));

        modelBuilder.Entity<Curso>()
            .HasQueryFilter(c => !c.IsDeleted && (CurrentSchoolId == null || c.ColegioId == CurrentSchoolId));

        modelBuilder.Entity<Admin>()
            .HasQueryFilter(a => !a.IsDeleted && (CurrentSchoolId == null || (a.Cuenta != null && a.Cuenta.ColegioId == CurrentSchoolId)));

        modelBuilder.Entity<Profesor>()
            .HasQueryFilter(p => !p.IsDeleted && (CurrentSchoolId == null || (p.Cuenta != null && p.Cuenta.ColegioId == CurrentSchoolId)));

        modelBuilder.Entity<Estudiante>()
            .HasQueryFilter(e => !e.IsDeleted && (CurrentSchoolId == null || (e.Curso != null && e.Curso.ColegioId == CurrentSchoolId)));

        modelBuilder.Entity<Asignatura>()
            .HasQueryFilter(a => !a.IsDeleted && (CurrentSchoolId == null || (a.Curso != null && a.Curso.ColegioId == CurrentSchoolId)));

        modelBuilder.Entity<EstudianteAsignatura>()
            .HasQueryFilter(ea => !ea.IsDeleted && (CurrentSchoolId == null || (ea.Asignatura != null && ea.Asignatura.Curso != null && ea.Asignatura.Curso.ColegioId == CurrentSchoolId)));

        modelBuilder.Entity<ProfesorAsignaturaCurso>()
            .HasQueryFilter(pac => !pac.IsDeleted && (CurrentSchoolId == null || (pac.Curso != null && pac.Curso.ColegioId == CurrentSchoolId)));

        modelBuilder.Entity<Nota>()
            .HasQueryFilter(n => !n.IsDeleted && (CurrentSchoolId == null || (n.Tarea != null && n.Tarea.Asignatura != null && n.Tarea.Asignatura.Curso != null && n.Tarea.Asignatura.Curso.ColegioId == CurrentSchoolId)));

        modelBuilder.Entity<Tarea>()
            .HasQueryFilter(t => !t.IsDeleted && (CurrentSchoolId == null || (t.Asignatura != null && t.Asignatura.Curso != null && t.Asignatura.Curso.ColegioId == CurrentSchoolId)));

        // Length constraints
        modelBuilder.Entity<Curso>().Property(c => c.Nombre).HasMaxLength(100);
        modelBuilder.Entity<Curso>().Property(c => c.ColegioId).HasDefaultValue(1);
        modelBuilder.Entity<Colegio>().Property(c => c.Nombre).HasMaxLength(160);
        modelBuilder.Entity<Colegio>().Property(c => c.Slug).HasMaxLength(80);
        modelBuilder.Entity<Colegio>().Property(c => c.LogoUrl).HasMaxLength(500);
        modelBuilder.Entity<Colegio>().Property(c => c.FaviconUrl).HasMaxLength(500);
        modelBuilder.Entity<Colegio>().Property(c => c.ColorPrimario).HasMaxLength(20);
        modelBuilder.Entity<Colegio>().Property(c => c.MensajeLogin).HasMaxLength(240);

        modelBuilder.Entity<Asignatura>().Property(a => a.Nombre).HasMaxLength(100);

        modelBuilder.Entity<Cuenta>().Property(c => c.Correo).HasMaxLength(255);
        modelBuilder.Entity<Cuenta>().Property(c => c.Rol).HasMaxLength(32);
        modelBuilder.Entity<Cuenta>().Property(c => c.ColegioId).HasDefaultValue(1);

        modelBuilder.Entity<Profesor>().Property(p => p.Nombre).HasMaxLength(150);
        modelBuilder.Entity<Profesor>().Property(p => p.Apellidos).HasMaxLength(150).IsRequired();
        modelBuilder.Entity<Profesor>().Property(p => p.DNI).HasMaxLength(20).IsRequired();
        modelBuilder.Entity<Profesor>().Property(p => p.Telefono).HasMaxLength(20).IsRequired();
        modelBuilder.Entity<Profesor>().Property(p => p.Especialidad).HasMaxLength(100).IsRequired();

        modelBuilder.Entity<Estudiante>().Property(e => e.Nombre).HasMaxLength(150);
        modelBuilder.Entity<Estudiante>().Property(e => e.Apellidos).HasMaxLength(150).IsRequired();
        modelBuilder.Entity<Estudiante>().Property(e => e.DNI).HasMaxLength(20).IsRequired();
        modelBuilder.Entity<Estudiante>().Property(e => e.Telefono).HasMaxLength(20).IsRequired();

        modelBuilder.Entity<Admin>().Property(a => a.Nombre).HasMaxLength(150);

        // FK performance indexes
        modelBuilder.Entity<Admin>().HasIndex(a => a.CuentaId).IsUnique();
        modelBuilder.Entity<Profesor>().HasIndex(p => p.CuentaId).IsUnique();
        modelBuilder.Entity<Estudiante>().HasIndex(e => e.CuentaId).IsUnique();

        modelBuilder.Entity<Estudiante>()
            .HasIndex(e => e.CursoId);

        modelBuilder.Entity<Nota>()
            .HasIndex(n => n.TareaId);

        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges()
    {
        ApplySoftDeleteRules();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplySoftDeleteRules();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplySoftDeleteRules();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplySoftDeleteRules();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private static void ConfigureSoftDelete<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ISoftDeletable
    {
        modelBuilder.Entity<TEntity>()
            .Property(x => x.IsDeleted)
            .HasDefaultValue(false);

        modelBuilder.Entity<TEntity>()
            .HasQueryFilter(x => !x.IsDeleted);
    }

    private void ApplySoftDeleteRules()
    {
        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.IsDeleted = false;
            }
            else if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Property(x => x.IsDeleted).IsModified = true;
            }
        }
    }
}
