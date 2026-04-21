using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Persistence.Context;
using Back.Api.Application.Dtos;
using Back.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Back.Api.Persistence.Repositories;

public class AsignaturasDomainRepository(AppDbContext context) : IAsignaturasDomainRepository
{
    public Task<bool> ExisteAsync(int asignaturaId, CancellationToken cancellationToken = default) =>
        context.Asignaturas.AnyAsync(a => a.Id == asignaturaId);

    public Task<bool> CursoExisteAsync(int cursoId, CancellationToken cancellationToken = default) =>
        context.Cursos.AnyAsync(c => c.Id == cursoId);

    public Task<bool> ExisteEnCursoAsync(int cursoId, string nombre, CancellationToken cancellationToken = default) =>
        context.Asignaturas.AnyAsync(a => a.CursoId == cursoId && a.Nombre == nombre);

    public Task<string?> GetCursoNombreAsync(int cursoId, CancellationToken cancellationToken = default) =>
        context.Cursos.AsNoTracking()
            .Where(c => c.Id == cursoId)
            .Select(c => (string?)c.Nombre)
            .FirstOrDefaultAsync();

    public async Task<IEnumerable<AsignaturaResumenDto>> GetAllResumenAsync(CancellationToken cancellationToken = default)
    {
        var asignaturasBase = await context.Asignaturas
            .AsNoTracking()
            .Select(a => new { a.Id, a.Nombre, CursoId = a.CursoId, CursoNombre = a.Curso!.Nombre })
            .ToListAsync(cancellationToken);

        var profesoresPorAsignatura = await context.ProfesorAsignaturaCursos
            .AsNoTracking()
            .Select(i => new { i.AsignaturaId, i.ProfesorId, ProfesorNombre = i.Profesor!.Nombre })
            .Distinct()
            .ToListAsync(cancellationToken);

        var alumnosPorAsignatura = await context.EstudianteAsignaturas
            .AsNoTracking()
            .Select(ea => new { ea.AsignaturaId, EstudianteId = ea.EstudianteId, EstudianteNombre = ea.Estudiante!.Nombre })
            .Distinct()
            .ToListAsync(cancellationToken);

        return asignaturasBase.Select(asignatura => new AsignaturaResumenDto
            {
            Id = asignatura.Id,
            Nombre = asignatura.Nombre,
            Curso = new AsignaturaCursoDto { Id = asignatura.CursoId, Nombre = asignatura.CursoNombre },
            Profesores = profesoresPorAsignatura
                    .Where(profesorAsignatura => profesorAsignatura.AsignaturaId == asignatura.Id)
                    .OrderBy(profesorAsignatura => profesorAsignatura.ProfesorNombre)
                    .Select(profesorAsignatura => new AsignaturaProfesorDto { ProfesorId = profesorAsignatura.ProfesorId, Nombre = profesorAsignatura.ProfesorNombre })
                .ToList(),
            Alumnos = alumnosPorAsignatura
                    .Where(alumnoAsignatura => alumnoAsignatura.AsignaturaId == asignatura.Id)
                    .OrderBy(alumnoAsignatura => alumnoAsignatura.EstudianteNombre)
                    .Select(alumnoAsignatura => new AsignaturaAlumnoLookupDto { Id = alumnoAsignatura.EstudianteId, Nombre = alumnoAsignatura.EstudianteNombre })
                .ToList()
        });
    }

    public async Task<AsignaturaDetalleDto?> GetDetalleAsync(int asignaturaId, CancellationToken cancellationToken = default)
    {
        var asignatura = await context.Asignaturas
            .AsNoTracking()
            .Where(a => a.Id == asignaturaId)
            .Select(a => new { a.Id, a.Nombre, CursoId = a.CursoId, Curso = a.Curso!.Nombre })
            .FirstOrDefaultAsync();

        if (asignatura is null) return null;

        var imparticiones = await context.ProfesorAsignaturaCursos
            .AsNoTracking()
            .Where(i => i.AsignaturaId == asignaturaId)
            .Select(i => new AsignaturaImparticionDto { ProfesorId = i.ProfesorId, Profesor = i.Profesor!.Nombre })
            .Distinct()
            .OrderBy(i => i.Profesor)
            .ToListAsync();

        var alumnos = await context.EstudianteAsignaturas
            .AsNoTracking()
            .Where(ea => ea.AsignaturaId == asignaturaId)
            .Select(ea => new
            {
                ea.EstudianteId,
                Alumno = ea.Estudiante!.Nombre,
                Notas = context.Notas
                        .Where(nota => nota.EstudianteId == ea.EstudianteId && nota.Tarea!.AsignaturaId == asignaturaId)
                        .Select(nota => new AsignaturaNotaResumenDto
                    {
                            Id = nota.Id,
                            Tarea = nota.Tarea!.Nombre,
                            Trimestre = nota.Tarea.Trimestre,
                            Valor = nota.Valor
                    })
                    .ToList()
            })
                .OrderBy(alumno => alumno.Alumno)
            .ToListAsync();

        return new AsignaturaDetalleDto
        {
            Id = asignatura.Id,
            Nombre = asignatura.Nombre,
            Curso = new AsignaturaCursoDto { Id = asignatura.CursoId, Nombre = asignatura.Curso },
            Imparticiones = imparticiones,
                Alumnos = alumnos.Select(alumno => new AsignaturaAlumnoDetalleDto
            {
                    EstudianteId = alumno.EstudianteId,
                    Alumno = alumno.Alumno,
                    Notas = alumno.Notas
            }).ToList()
        };
    }

    #region Mutations

    public async Task<AsignaturaResumenDto> CreateAsignaturaAsync(string nombre, int cursoId, CancellationToken cancellationToken = default)
    {
        var cursoNombre = await GetCursoNombreAsync(cursoId) ?? string.Empty;
        var asignatura = await context.Asignaturas
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.CursoId == cursoId && a.Nombre == nombre, cancellationToken);

        if (asignatura is null)
        {
            asignatura = new Asignatura { Nombre = nombre, CursoId = cursoId };
            context.Asignaturas.Add(asignatura);
        }
        else
        {
            asignatura.Nombre = nombre;
            asignatura.CursoId = cursoId;
            asignatura.IsDeleted = false;
        }

        await context.SaveChangesAsync(cancellationToken);
        return new AsignaturaResumenDto
        {
            Id = asignatura.Id,
            Nombre = asignatura.Nombre,
            Curso = new AsignaturaCursoDto { Id = cursoId, Nombre = cursoNombre },
            Profesores = new(),
            Alumnos = new()
        };
    }

    public async Task<AsignaturaResumenDto?> UpdateAsignaturaAsync(int asignaturaId, string nombre, int cursoId, CancellationToken cancellationToken = default)
    {
        var asignatura = await context.Asignaturas.FirstOrDefaultAsync(a => a.Id == asignaturaId, cancellationToken);
        if (asignatura is null) return null;
        asignatura.Nombre = nombre;
        asignatura.CursoId = cursoId;
        await context.SaveChangesAsync(cancellationToken);
        var cursoNombre = await GetCursoNombreAsync(cursoId) ?? string.Empty;
        return new AsignaturaResumenDto
        {
            Id = asignatura.Id,
            Nombre = asignatura.Nombre,
            Curso = new AsignaturaCursoDto { Id = cursoId, Nombre = cursoNombre },
            Profesores = new(),
            Alumnos = new()
        };
    }

    public async Task DeleteAsignaturaAsync(int asignaturaId, CancellationToken cancellationToken = default)
    {
        var imparticiones = await context.ProfesorAsignaturaCursos
            .Where(i => i.AsignaturaId == asignaturaId).ToListAsync(cancellationToken);
        foreach (var imparticion in imparticiones) imparticion.IsDeleted = true;

        var matriculas = await context.EstudianteAsignaturas
            .Where(ea => ea.AsignaturaId == asignaturaId).ToListAsync(cancellationToken);
        foreach (var matricula in matriculas) matricula.IsDeleted = true;

        var tareaIds = await context.Tareas
            .Where(t => t.AsignaturaId == asignaturaId).Select(t => t.Id).ToListAsync(cancellationToken);
        if (tareaIds.Count > 0)
        {
            var notas = await context.Notas.Where(n => tareaIds.Contains(n.TareaId)).ToListAsync(cancellationToken);
            foreach (var nota in notas) nota.IsDeleted = true;

            var tareas = await context.Tareas.Where(t => t.AsignaturaId == asignaturaId).ToListAsync(cancellationToken);
            foreach (var tarea in tareas) tarea.IsDeleted = true;
        }

        var asignatura = await context.Asignaturas.FirstOrDefaultAsync(a => a.Id == asignaturaId, cancellationToken);
        if (asignatura is not null) asignatura.IsDeleted = true;
        await context.SaveChangesAsync(cancellationToken);
    }

    #endregion
}

