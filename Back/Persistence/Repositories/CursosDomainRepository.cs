using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Persistence.Context;
using Back.Api.Application.Dtos;
using Back.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Back.Api.Persistence.Repositories;

public class CursosDomainRepository(AppDbContext context) : ICursosDomainRepository
{
    public Task<bool> ExisteAsync(int cursoId, CancellationToken cancellationToken = default) =>
        context.Cursos.AnyAsync(c => c.Id == cursoId);

    public Task<bool> TieneEstudiantesAsync(int cursoId, CancellationToken cancellationToken = default) =>
        context.Estudiantes.AnyAsync(e => e.CursoId == cursoId);

    public Task<bool> TieneAsignaturasAsync(int cursoId, CancellationToken cancellationToken = default) =>
        context.Asignaturas.AnyAsync(a => a.CursoId == cursoId);

    public Task<CursoLookupDto?> GetCursoLookupAsync(int cursoId, CancellationToken cancellationToken = default) =>
        context.Cursos.AsNoTracking()
            .Where(c => c.Id == cursoId)
            .Select(c => new CursoLookupDto { Id = c.Id, Nombre = c.Nombre })
            .FirstOrDefaultAsync();

    public async Task<IEnumerable<CursoResumenDto>> GetAllResumenAsync(CancellationToken cancellationToken = default)
    {
        var cursosBase = await context.Cursos
            .AsNoTracking()
            .Select(c => new CursoLookupDto { Id = c.Id, Nombre = c.Nombre })
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

        return cursosBase.Select(curso => new CursoResumenDto
        {
            Id = curso.Id,
            Nombre = curso.Nombre,
            Asignaturas = asignaturasConProfesor
                .Where(asignatura => asignatura.CursoId == curso.Id)
                .Select(asignatura => new CursoAsignaturaDto
                {
                    Id = asignatura.Id,
                    Nombre = asignatura.Nombre,
                    ProfesorId = asignatura.ProfesorId,
                    Profesor = asignatura.Profesor
                })
                .ToList()
        });
    }

    public async Task<CursoDetalleDto?> GetDetalleAsync(int cursoId, CancellationToken cancellationToken = default)
    {
        var cursoLookup = await context.Cursos
            .AsNoTracking()
            .Where(c => c.Id == cursoId)
            .Select(c => new CursoLookupDto { Id = c.Id, Nombre = c.Nombre })
            .FirstOrDefaultAsync();

        if (cursoLookup is null) return null;

        var alumnos = await context.Estudiantes
            .AsNoTracking()
            .Where(e => e.CursoId == cursoId)
            .Select(e => new CursoAlumnoDto { Id = e.Id, Nombre = e.Nombre })
            .OrderBy(e => e.Nombre)
            .ToListAsync();

        var asignaturas = await context.Asignaturas
            .AsNoTracking()
            .Where(a => a.CursoId == cursoId)
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
                    .OrderBy(estudianteId => estudianteId)
                    .ToList()
            })
            .OrderBy(a => a.Nombre)
            .ToListAsync();

        return new CursoDetalleDto
        {
            Id = cursoLookup.Id,
            Nombre = cursoLookup.Nombre,
            Alumnos = alumnos,
            Asignaturas = asignaturas
        };
    }

    #region Mutations

    public async Task<CursoLookupDto> CreateCursoAsync(string nombre, CancellationToken cancellationToken = default)
    {
        var existingCurso = await context.Cursos
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Nombre == nombre, cancellationToken);

        if (existingCurso is null)
        {
            existingCurso = new Curso { Nombre = nombre };
            context.Cursos.Add(existingCurso);
        }
        else
        {
            existingCurso.Nombre = nombre;
            existingCurso.IsDeleted = false;
        }

        await context.SaveChangesAsync(cancellationToken);
        return new CursoLookupDto { Id = existingCurso.Id, Nombre = existingCurso.Nombre };
    }

    public async Task<CursoLookupDto?> UpdateCursoAsync(int cursoId, string nombre, CancellationToken cancellationToken = default)
    {
        var cursoToUpdate = await context.Cursos.FirstOrDefaultAsync(c => c.Id == cursoId, cancellationToken);
        if (cursoToUpdate is null) return null;
        cursoToUpdate.Nombre = nombre;
        await context.SaveChangesAsync(cancellationToken);
        return new CursoLookupDto { Id = cursoToUpdate.Id, Nombre = cursoToUpdate.Nombre };
    }

    public async Task DeleteCursoAsync(int cursoId, CancellationToken cancellationToken = default)
    {
        var cursoToDelete = await context.Cursos.FirstOrDefaultAsync(c => c.Id == cursoId, cancellationToken);
        if (cursoToDelete is not null)
        {
            cursoToDelete.IsDeleted = true;
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    #endregion
}

