using Back.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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
        modelBuilder.Entity<Profesor>()
            .HasIndex(p => p.Correo)
            .IsUnique();

        modelBuilder.Entity<Admin>()
            .HasIndex(a => a.Correo)
            .IsUnique();

        modelBuilder.Entity<Estudiante>()
            .HasIndex(e => e.Correo)
            .IsUnique();

        modelBuilder.Entity<Asignatura>()
            .HasOne(a => a.Curso)
            .WithMany()
            .HasForeignKey(a => a.CursoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Asignatura>()
            .HasIndex(a => new { a.CursoId, a.Nombre })
            .IsUnique();

        modelBuilder.Entity<EstudianteAsignatura>()
            .HasKey(x => new { x.EstudianteId, x.AsignaturaId });

        modelBuilder.Entity<ProfesorAsignaturaCurso>()
            .HasKey(x => new { x.ProfesorId, x.AsignaturaId, x.CursoId });

        modelBuilder.Entity<Nota>()
            .Property(x => x.Valor)
            .HasPrecision(4, 2);

        modelBuilder.Entity<Nota>()
            .HasIndex(x => new { x.EstudianteId, x.TareaId })
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(x => x.Token)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(x => new { x.UserId, x.Rol });

        base.OnModelCreating(modelBuilder);
    }
}
