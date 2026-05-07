using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Persistence.Context;
using Back.Api.Application.Dtos;
using Back.Api.Application.Abstractions.Security;
using Back.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Back.Api.Persistence.Repositories;

public class CursosDomainRepository(AppDbContext context, ICurrentSchoolContext currentSchoolContext) : ICursosDomainRepository
{
    public Task<bool> ExisteAsync(int cursoId, CancellationToken cancellationToken = default) =>
        context.Cursos.AnyAsync(c => c.Id == cursoId, cancellationToken);

    public Task<bool> TieneEstudiantesAsync(int cursoId, CancellationToken cancellationToken = default) =>
        context.Estudiantes.AnyAsync(e => e.CursoId == cursoId, cancellationToken);

    public Task<bool> TieneAsignaturasAsync(int cursoId, CancellationToken cancellationToken = default) =>
        context.Asignaturas.AnyAsync(a => a.CursoId == cursoId, cancellationToken);

    public Task<CursoLookupDto?> GetCursoLookupAsync(int cursoId, CancellationToken cancellationToken = default) =>
        context.Cursos.AsNoTracking()
            .Where(c => c.Id == cursoId)
            .Select(c => new CursoLookupDto { Id = c.Id, Nombre = c.Nombre })
            .FirstOrDefaultAsync(cancellationToken);

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

        var subjectsByCourseId = asignaturasConProfesor
            .GroupBy(subject => subject.CursoId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(subject => new CursoAsignaturaDto
                    {
                        Id = subject.Id,
                        Nombre = subject.Nombre,
                        ProfesorId = subject.ProfesorId,
                        Profesor = subject.Profesor
                    })
                    .ToList());

        return cursosBase.Select(course => new CursoResumenDto
        {
            Id = course.Id,
            Nombre = course.Nombre,
            Asignaturas = subjectsByCourseId.GetValueOrDefault(course.Id, [])
        });
    }

    public async Task<CursoDetalleDto?> GetDetalleAsync(int cursoId, CancellationToken cancellationToken = default)
    {
        var courseLookup = await context.Cursos
            .AsNoTracking()
            .Where(c => c.Id == cursoId)
            .Select(c => new CursoLookupDto { Id = c.Id, Nombre = c.Nombre })
            .FirstOrDefaultAsync(cancellationToken);

        if (courseLookup is null) return null;

        var alumnos = await context.Estudiantes
            .AsNoTracking()
            .Where(e => e.CursoId == cursoId)
            .Select(e => new CursoAlumnoDto { Id = e.Id, Nombre = e.Nombre })
            .OrderBy(e => e.Nombre)
            .ToListAsync(cancellationToken);

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
            .ToListAsync(cancellationToken);

        return new CursoDetalleDto
        {
            Id = courseLookup.Id,
            Nombre = courseLookup.Nombre,
            Alumnos = alumnos,
            Asignaturas = asignaturas
        };
    }

    #region Mutations

    public async Task<CursoLookupDto> CreateCursoAsync(string nombre, CancellationToken cancellationToken = default)
    {
        var existingCourse = await context.Cursos
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Nombre == nombre && c.ColegioId == (currentSchoolContext.SchoolId ?? 1), cancellationToken);

        if (existingCourse is null)
        {
            existingCourse = new Curso { Nombre = nombre, ColegioId = currentSchoolContext.SchoolId ?? 1 };
            context.Cursos.Add(existingCourse);
        }
        else
        {
            existingCourse.Nombre = nombre;
            existingCourse.IsDeleted = false;
        }

        await context.SaveChangesAsync(cancellationToken);
        return new CursoLookupDto { Id = existingCourse.Id, Nombre = existingCourse.Nombre };
    }

    public async Task<CursoLookupDto?> UpdateCursoAsync(int cursoId, string nombre, CancellationToken cancellationToken = default)
    {
        var courseToUpdate = await context.Cursos.FirstOrDefaultAsync(c => c.Id == cursoId, cancellationToken);
        if (courseToUpdate is null) return null;
        courseToUpdate.Nombre = nombre;
        await context.SaveChangesAsync(cancellationToken);
        return new CursoLookupDto { Id = courseToUpdate.Id, Nombre = courseToUpdate.Nombre };
    }

    public async Task DeleteCursoAsync(int cursoId, CancellationToken cancellationToken = default)
    {
        var courseToDelete = await context.Cursos.FirstOrDefaultAsync(c => c.Id == cursoId, cancellationToken);
        if (courseToDelete is not null)
        {
            courseToDelete.IsDeleted = true;
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    #endregion
}

