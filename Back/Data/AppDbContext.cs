using Back.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Back.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Curso> Cursos => Set<Curso>();
    public DbSet<Estudiante> Estudiantes => Set<Estudiante>();
    public DbSet<Profesor> Profesores => Set<Profesor>();
    public DbSet<Asignatura> Asignaturas => Set<Asignatura>();
    public DbSet<EstudianteAsignatura> EstudianteAsignaturas => Set<EstudianteAsignatura>();
    public DbSet<ProfesorAsignaturaCurso> ProfesorAsignaturaCursos => Set<ProfesorAsignaturaCurso>();
    public DbSet<Nota> Notas => Set<Nota>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
            .HasIndex(x => new { x.EstudianteId, x.AsignaturaId })
            .IsUnique();

        base.OnModelCreating(modelBuilder);
    }
}
