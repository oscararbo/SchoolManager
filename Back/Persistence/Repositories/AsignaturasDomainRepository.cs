using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Persistence.Context;
using Back.Api.Application.Dtos;
using Back.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Back.Api.Persistence.Repositories;

public class AsignaturasDomainRepository(AppDbContext context) : IAsignaturasDomainRepository
{
    public Task<bool> ExisteAsync(int id, CancellationToken cancellationToken = default) =>
        context.Asignaturas.AnyAsync(a => a.Id == id);

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

        return asignaturasBase.Select(a => new AsignaturaResumenDto
        {
            Id = a.Id,
            Nombre = a.Nombre,
            Curso = new AsignaturaCursoDto { Id = a.CursoId, Nombre = a.CursoNombre },
            Profesores = profesoresPorAsignatura
                .Where(p => p.AsignaturaId == a.Id)
                .OrderBy(p => p.ProfesorNombre)
                .Select(p => new AsignaturaProfesorDto { ProfesorId = p.ProfesorId, Nombre = p.ProfesorNombre })
                .ToList(),
            Alumnos = alumnosPorAsignatura
                .Where(e => e.AsignaturaId == a.Id)
                .OrderBy(e => e.EstudianteNombre)
                .Select(e => new AsignaturaAlumnoLookupDto { Id = e.EstudianteId, Nombre = e.EstudianteNombre })
                .ToList()
        });
    }

    public async Task<AsignaturaDetalleDto?> GetDetalleAsync(int id, CancellationToken cancellationToken = default)
    {
        var asignatura = await context.Asignaturas
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new { a.Id, a.Nombre, CursoId = a.CursoId, Curso = a.Curso!.Nombre })
            .FirstOrDefaultAsync();

        if (asignatura is null) return null;

        var imparticiones = await context.ProfesorAsignaturaCursos
            .AsNoTracking()
            .Where(i => i.AsignaturaId == id)
            .Select(i => new AsignaturaImparticionDto { ProfesorId = i.ProfesorId, Profesor = i.Profesor!.Nombre })
            .Distinct()
            .OrderBy(i => i.Profesor)
            .ToListAsync();

        var alumnos = await context.EstudianteAsignaturas
            .AsNoTracking()
            .Where(ea => ea.AsignaturaId == id)
            .Select(ea => new
            {
                ea.EstudianteId,
                Alumno = ea.Estudiante!.Nombre,
                Notas = context.Notas
                    .Where(n => n.EstudianteId == ea.EstudianteId && n.Tarea!.AsignaturaId == id)
                    .Select(n => new AsignaturaNotaResumenDto
                    {
                        Id = n.Id,
                        Tarea = n.Tarea!.Nombre,
                        Trimestre = n.Tarea.Trimestre,
                        Valor = n.Valor
                    })
                    .ToList()
            })
            .OrderBy(x => x.Alumno)
            .ToListAsync();

        return new AsignaturaDetalleDto
        {
            Id = asignatura.Id,
            Nombre = asignatura.Nombre,
            Curso = new AsignaturaCursoDto { Id = asignatura.CursoId, Nombre = asignatura.Curso },
            Imparticiones = imparticiones,
            Alumnos = alumnos.Select(a => new AsignaturaAlumnoDetalleDto
            {
                EstudianteId = a.EstudianteId,
                Alumno = a.Alumno,
                Notas = a.Notas
            }).ToList()
        };
    }

    #region Mutations

    public async Task<AsignaturaResumenDto> CreateAsync(string nombre, int cursoId, CancellationToken cancellationToken = default)
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

    public async Task<AsignaturaResumenDto?> UpdateAsync(int id, string nombre, int cursoId, CancellationToken cancellationToken = default)
    {
        var asignatura = await context.Asignaturas.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
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

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var imparticiones = await context.ProfesorAsignaturaCursos
            .Where(i => i.AsignaturaId == id).ToListAsync(cancellationToken);
        foreach (var imparticion in imparticiones) imparticion.IsDeleted = true;

        var matriculas = await context.EstudianteAsignaturas
            .Where(ea => ea.AsignaturaId == id).ToListAsync(cancellationToken);
        foreach (var matricula in matriculas) matricula.IsDeleted = true;

        var tareaIds = await context.Tareas
            .Where(t => t.AsignaturaId == id).Select(t => t.Id).ToListAsync(cancellationToken);
        if (tareaIds.Count > 0)
        {
            var notas = await context.Notas.Where(n => tareaIds.Contains(n.TareaId)).ToListAsync(cancellationToken);
            foreach (var nota in notas) nota.IsDeleted = true;

            var tareas = await context.Tareas.Where(t => t.AsignaturaId == id).ToListAsync(cancellationToken);
            foreach (var tarea in tareas) tarea.IsDeleted = true;
        }

        var asignatura = await context.Asignaturas.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (asignatura is not null) asignatura.IsDeleted = true;
        await context.SaveChangesAsync(cancellationToken);
    }

    #endregion
}

