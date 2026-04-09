using Back.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Back.Api.Persistence.Context;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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

        modelBuilder.Entity<Profesor>()
            .HasIndex(p => p.Correo)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        modelBuilder.Entity<Admin>()
            .HasIndex(a => a.Correo)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        modelBuilder.Entity<Estudiante>()
            .HasIndex(e => e.Correo)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        modelBuilder.Entity<Asignatura>()
            .HasOne(a => a.Curso)
            .WithMany()
            .HasForeignKey(a => a.CursoId)
            .OnDelete(DeleteBehavior.Restrict);

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
                continue;
            }

            if (entry.State != EntityState.Deleted)
            {
                continue;
            }

            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Property(x => x.IsDeleted).IsModified = true;
        }
    }
}
