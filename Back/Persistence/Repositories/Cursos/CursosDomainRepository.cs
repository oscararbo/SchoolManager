using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Persistence.Context;
using Back.Api.Application.Dtos;
using Back.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Back.Api.Persistence.Repositories;

public class CursosDomainRepository(AppDbContext context) : ICursosDomainRepository
{
    public Task<bool> ExisteAsync(int id, CancellationToken cancellationToken = default) =>
        context.Cursos.AnyAsync(c => c.Id == id);

    public Task<bool> TieneEstudiantesAsync(int id, CancellationToken cancellationToken = default) =>
        context.Estudiantes.AnyAsync(e => e.CursoId == id);

    public Task<bool> TieneAsignaturasAsync(int id, CancellationToken cancellationToken = default) =>
        context.Asignaturas.AnyAsync(a => a.CursoId == id);

    public Task<CursoSimpleDto?> GetSimpleAsync(int id, CancellationToken cancellationToken = default) =>
        context.Cursos.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CursoSimpleDto { Id = c.Id, Nombre = c.Nombre })
            .FirstOrDefaultAsync();

    public async Task<IEnumerable<CursoResumenDto>> GetAllResumenAsync(CancellationToken cancellationToken = default)
    {
        var cursosBase = await context.Cursos
            .AsNoTracking()
            .Select(c => new CursoSimpleDto { Id = c.Id, Nombre = c.Nombre })
            .OrderBy(c => c.Nombre)
            .ToListAsync(cancellationToken);

        var asignaturasConProfesor = await context.Asignaturas
            .AsNoTracking()
            .Select(a => new
            {
                a.Id,
                a.Nombre,
                a.CursoId,
                ProfesorId = a.ProfesorAsignaturaCursos
                    .Select(pac => (int?)pac.ProfesorId)
                    .FirstOrDefault(),
                Profesor = a.ProfesorAsignaturaCursos
                    .Select(pac => pac.Profesor!.Nombre)
                    .FirstOrDefault()
            })
            .OrderBy(a => a.Nombre)
            .ToListAsync(cancellationToken);

        return cursosBase.Select(c => new CursoResumenDto
        {
            Id = c.Id,
            Nombre = c.Nombre,
            Asignaturas = asignaturasConProfesor
                .Where(a => a.CursoId == c.Id)
                .Select(a => new CursoAsignaturaDto
                {
                    Id = a.Id,
                    Nombre = a.Nombre,
                    ProfesorId = a.ProfesorId,
                    Profesor = a.Profesor
                })
                .ToList()
        });
    }

    public async Task<CursoDetalleDto?> GetDetalleAsync(int id, CancellationToken cancellationToken = default)
    {
        var curso = await context.Cursos
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CursoSimpleDto { Id = c.Id, Nombre = c.Nombre })
            .FirstOrDefaultAsync();

        if (curso is null) return null;

        var alumnos = await context.Estudiantes
            .AsNoTracking()
            .Where(e => e.CursoId == id)
            .Select(e => new CursoAlumnoDto { Id = e.Id, Nombre = e.Nombre })
            .OrderBy(e => e.Nombre)
            .ToListAsync();

        var asignaturas = await context.Asignaturas
            .AsNoTracking()
            .Where(a => a.CursoId == id)
            .Select(a => new CursoDetalleAsignaturaDto
            {
                Id = a.Id,
                Nombre = a.Nombre,
                ProfesorId = a.ProfesorAsignaturaCursos
                    .Select(pac => (int?)pac.ProfesorId)
                    .FirstOrDefault(),
                Profesor = a.ProfesorAsignaturaCursos
                    .Select(pac => pac.Profesor!.Nombre)
                    .FirstOrDefault(),
                AlumnosMatriculadosIds = a.EstudianteAsignaturas
                    .Select(ea => ea.EstudianteId)
                    .OrderBy(x => x)
                    .ToList()
            })
            .OrderBy(a => a.Nombre)
            .ToListAsync();

        return new CursoDetalleDto
        {
            Id = curso.Id,
            Nombre = curso.Nombre,
            Alumnos = alumnos,
            Asignaturas = asignaturas
        };
    }

    // ── Mutations ─────────────────────────────────────────────────────────────

    public async Task<CursoSimpleDto> CreateAsync(string nombre, CancellationToken cancellationToken = default)
    {
        var curso = new Curso { Nombre = nombre };
        context.Cursos.Add(curso);
        await context.SaveChangesAsync();
        return new CursoSimpleDto { Id = curso.Id, Nombre = curso.Nombre };
    }

    public async Task<CursoSimpleDto?> UpdateAsync(int id, string nombre, CancellationToken cancellationToken = default)
    {
        var curso = await context.Cursos.FindAsync(id);
        if (curso is null) return null;
        curso.Nombre = nombre;
        await context.SaveChangesAsync();
        return new CursoSimpleDto { Id = curso.Id, Nombre = curso.Nombre };
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var curso = await context.Cursos.FindAsync(id);
        if (curso is not null)
        {
            context.Cursos.Remove(curso);
            await context.SaveChangesAsync();
        }
    }
}
