using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Persistence.Context;
using Back.Api.Application.Dtos;
using Back.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Back.Api.Persistence.Repositories;

public class EstudiantesDomainRepository(AppDbContext context) : IEstudiantesDomainRepository
{
    public Task<bool> ExisteAsync(int id, CancellationToken cancellationToken = default) =>
        context.Estudiantes.AnyAsync(e => e.Id == id);

    public Task<bool> CorreoDuplicadoAsync(string correo, CancellationToken cancellationToken = default) =>
        context.Estudiantes.AnyAsync(e => e.Correo == correo);

    public Task<bool> CorreoDuplicadoExceptAsync(string correo, int exceptId, CancellationToken cancellationToken = default) =>
        context.Estudiantes.AnyAsync(e => e.Correo == correo && e.Id != exceptId);

    public Task<bool> CursoExisteAsync(int cursoId, CancellationToken cancellationToken = default) =>
        context.Cursos.AnyAsync(c => c.Id == cursoId);

    public Task<bool> AsignaturaExisteAsync(int asignaturaId, CancellationToken cancellationToken = default) =>
        context.Asignaturas.AnyAsync(a => a.Id == asignaturaId);

    public Task<bool> YaMatriculadoAsync(int estudianteId, int asignaturaId, CancellationToken cancellationToken = default) =>
        context.EstudianteAsignaturas.AnyAsync(x => x.EstudianteId == estudianteId && x.AsignaturaId == asignaturaId);

    public async Task<bool> AsignaturaEsDelCursoAsync(int asignaturaId, int cursoId, CancellationToken cancellationToken = default)
    {
        var cursoDeLaAsignatura = await context.Asignaturas
            .AsNoTracking()
            .Where(a => a.Id == asignaturaId)
            .Select(a => a.CursoId)
            .FirstOrDefaultAsync();

        return cursoDeLaAsignatura == cursoId;
    }

    public async Task<AlumnoPanelResumenDto?> GetPanelResumenAsync(int id, CancellationToken cancellationToken = default)
    {
        var estudiante = await context.Estudiantes
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new { e.Id, e.Nombre, e.CursoId, Curso = e.Curso!.Nombre })
            .FirstOrDefaultAsync();

        if (estudiante is null) return null;

        var materias = await context.EstudianteAsignaturas
            .AsNoTracking()
            .Where(ea => ea.EstudianteId == id)
            .Select(ea => new AlumnoMateriaResumenDto
            {
                AsignaturaId = ea.AsignaturaId,
                Asignatura = ea.Asignatura!.Nombre,
                Profesor = ea.Asignatura.ProfesorAsignaturaCursos
                    .Where(pac => pac.CursoId == estudiante.CursoId)
                    .Select(pac => pac.Profesor!.Nombre)
                    .FirstOrDefault()
            })
            .OrderBy(m => m.Asignatura)
            .ToListAsync();

        return new AlumnoPanelResumenDto
        {
            Id = estudiante.Id,
            Nombre = estudiante.Nombre,
            Curso = new AlumnoCursoDto { CursoId = estudiante.CursoId, Curso = estudiante.Curso },
            Materias = materias
        };
    }

    public async Task<AlumnoMateriaDetalleDto?> GetMateriaDetalleAsync(int estudianteId, int asignaturaId, CancellationToken cancellationToken = default)
    {
        var estudiante = await context.Estudiantes
            .AsNoTracking()
            .Where(e => e.Id == estudianteId)
            .Select(e => new { e.CursoId })
            .FirstOrDefaultAsync();

        if (estudiante is null) return null;

        var asignatura = await context.Asignaturas
            .AsNoTracking()
            .Where(a => a.Id == asignaturaId)
            .Select(a => new
            {
                a.Id,
                a.Nombre,
                Profesor = a.ProfesorAsignaturaCursos
                    .Where(pac => pac.CursoId == estudiante.CursoId)
                    .Select(pac => pac.Profesor!.Nombre)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (asignatura is null) return null;

        var tareas = await context.Tareas
            .AsNoTracking()
            .Where(t => t.AsignaturaId == asignaturaId)
            .OrderBy(t => t.Trimestre)
            .ThenBy(t => t.Nombre)
            .ToListAsync();

        var tareaIds = tareas.Select(t => t.Id).ToList();
        var notasAlumno = await context.Notas
            .AsNoTracking()
            .Where(n => n.EstudianteId == estudianteId && tareaIds.Contains(n.TareaId))
            .ToListAsync();

        var notasList = tareas.Select(t =>
        {
            var nota = notasAlumno.FirstOrDefault(n => n.TareaId == t.Id);
            return new AlumnoTareaDto
            {
                TareaId = t.Id,
                Nombre = t.Nombre,
                Trimestre = t.Trimestre,
                Valor = nota?.Valor
            };
        }).ToList();

        decimal? Media(int trim)
        {
            var vals = notasList
                .Where(n => n.Trimestre == trim && n.Valor.HasValue)
                .Select(n => n.Valor!.Value)
                .ToList();
            return vals.Count > 0 ? Math.Round(vals.Average(), 2) : null;
        }

        var t1 = Media(1);
        var t2 = Media(2);
        var t3 = Media(3);
        decimal? notaFinal = (t1.HasValue && t2.HasValue && t3.HasValue)
            ? Math.Round((t1.Value + t2.Value + t3.Value) / 3, 2)
            : null;

        return new AlumnoMateriaDetalleDto
        {
            AsignaturaId = asignatura.Id,
            Asignatura = asignatura.Nombre,
            Profesor = asignatura.Profesor,
            Notas = notasList,
            Medias = new MediasTrimestralesDto { T1 = t1, T2 = t2, T3 = t3 },
            NotaFinal = notaFinal
        };
    }

    #region New queries

    public async Task<string?> GetCursoNombreAsync(int cursoId, CancellationToken cancellationToken = default)
        => await context.Cursos.AsNoTracking()
            .Where(c => c.Id == cursoId)
            .Select(c => (string?)c.Nombre)
            .FirstOrDefaultAsync();

    public async Task<IEnumerable<EstudianteListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => await context.Estudiantes
            .AsNoTracking()
            .Select(e => new EstudianteListItemDto
            {
                Id = e.Id,
                Nombre = e.Nombre,
                Correo = e.Correo,
                CursoId = e.CursoId,
                Curso = e.Curso != null ? e.Curso.Nombre : null
            })
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<EstudianteSimpleDto>> GetSimpleAsync(CancellationToken cancellationToken = default)
        => await context.Estudiantes
            .AsNoTracking()
            .OrderBy(e => e.Nombre)
            .Select(e => new EstudianteSimpleDto
            {
                Id = e.Id,
                Nombre = e.Nombre,
                CursoId = e.CursoId,
                Curso = e.Curso != null ? e.Curso.Nombre : null
            })
            .ToListAsync(cancellationToken);

    public async Task<EstudianteDetalleDto?> GetDetalleAsync(int id, CancellationToken cancellationToken = default)
        => await context.Estudiantes
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new EstudianteDetalleDto
            {
                Id = e.Id,
                Nombre = e.Nombre,
                Correo = e.Correo,
                CursoId = e.CursoId,
                Curso = e.Curso != null ? e.Curso.Nombre : null,
                Asignaturas = e.EstudianteAsignaturas.Select(x => new EstudianteAsignaturaDetalleDto
                {
                    AsignaturaId = x.AsignaturaId,
                    Nombre = x.Asignatura!.Nombre,
                    ProfesorId = x.Asignatura.ProfesorAsignaturaCursos
                        .Where(pac => pac.CursoId == e.CursoId)
                        .Select(pac => (int?)pac.ProfesorId)
                        .FirstOrDefault(),
                    Profesor = x.Asignatura.ProfesorAsignaturaCursos
                        .Where(pac => pac.CursoId == e.CursoId)
                        .Select(pac => pac.Profesor!.Nombre)
                        .FirstOrDefault()
                }).ToList(),
                Notas = e.Notas.Select(n => new EstudianteNotaDetalleDto
                {
                    TareaId = n.TareaId,
                    Tarea = n.Tarea!.Nombre,
                    AsignaturaId = n.Tarea.AsignaturaId,
                    Asignatura = n.Tarea.Asignatura!.Nombre,
                    Trimestre = n.Tarea.Trimestre,
                    Valor = n.Valor,
                    ProfesorId = n.Tarea.ProfesorId,
                    Profesor = n.Tarea.Profesor!.Nombre
                }).ToList()
            })
            .FirstOrDefaultAsync();

    public async Task<AlumnoPanelDto?> GetPanelAlumnoAsync(int id, CancellationToken cancellationToken = default)
    {
        var estudiante = await context.Estudiantes
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new { e.Id, e.Nombre, e.CursoId, Curso = e.Curso!.Nombre })
            .FirstOrDefaultAsync();

        if (estudiante is null) return null;

        var asignaturaIds = await context.EstudianteAsignaturas
            .Where(ea => ea.EstudianteId == id)
            .Select(ea => ea.AsignaturaId)
            .ToListAsync();

        var materias = new List<AlumnoMateriaDto>();
        foreach (var asignaturaId in asignaturaIds)
        {
            var asignatura = await context.Asignaturas
                .AsNoTracking()
                .Where(a => a.Id == asignaturaId)
                .Select(a => new
                {
                    a.Id, a.Nombre,
                    Profesor = a.ProfesorAsignaturaCursos
                        .Where(pac => pac.CursoId == estudiante.CursoId)
                        .Select(pac => pac.Profesor!.Nombre)
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            if (asignatura is null) continue;

            var tareas = await context.Tareas
                .AsNoTracking()
                .Where(t => t.AsignaturaId == asignaturaId)
                .OrderBy(t => t.Trimestre).ThenBy(t => t.Nombre)
                .ToListAsync();

            var tareaIds = tareas.Select(t => t.Id).ToList();
            var notasAlumno = await context.Notas
                .AsNoTracking()
                .Where(n => n.EstudianteId == id && tareaIds.Contains(n.TareaId))
                .ToListAsync();

            var notasList = tareas.Select(t =>
            {
                var nota = notasAlumno.FirstOrDefault(n => n.TareaId == t.Id);
                return new AlumnoTareaDto { TareaId = t.Id, Nombre = t.Nombre, Trimestre = t.Trimestre, Valor = nota?.Valor };
            }).ToList();

            decimal? Media(int trim)
            {
                var vals = notasList.Where(n => n.Trimestre == trim && n.Valor.HasValue).Select(n => n.Valor!.Value).ToList();
                return vals.Count > 0 ? Math.Round(vals.Average(), 2) : null;
            }

            var t1 = Media(1); var t2 = Media(2); var t3 = Media(3);
            decimal? notaFinal = (t1.HasValue && t2.HasValue && t3.HasValue)
                ? Math.Round((t1.Value + t2.Value + t3.Value) / 3, 2) : null;

            materias.Add(new AlumnoMateriaDto
            {
                AsignaturaId = asignatura.Id,
                Asignatura = asignatura.Nombre,
                Profesor = asignatura.Profesor,
                Notas = notasList,
                Medias = new MediasTrimestralesDto { T1 = t1, T2 = t2, T3 = t3 },
                NotaFinal = notaFinal
            });
        }

        return new AlumnoPanelDto
        {
            Id = estudiante.Id,
            Nombre = estudiante.Nombre,
            Curso = new AlumnoCursoDto { CursoId = estudiante.CursoId, Curso = estudiante.Curso },
            Materias = materias.OrderBy(m => m.Asignatura).ToList()
        };
    }

    #endregion

    #region Mutations

    public async Task<EstudianteListItemDto> CreateAsync(string nombre, string correo, int cursoId, string hash, CancellationToken cancellationToken = default)
    {
        var estudiante = await context.Estudiantes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Correo == correo, cancellationToken);

        if (estudiante is null)
        {
            estudiante = new Estudiante { Nombre = nombre, Correo = correo, CursoId = cursoId, Contrasena = hash };
            context.Estudiantes.Add(estudiante);
        }
        else
        {
            estudiante.Nombre = nombre;
            estudiante.Correo = correo;
            estudiante.CursoId = cursoId;
            estudiante.Contrasena = hash;
            estudiante.IsDeleted = false;
        }

        await context.SaveChangesAsync();
        var cursoNombre = await GetCursoNombreAsync(cursoId);
        return new EstudianteListItemDto { Id = estudiante.Id, Nombre = estudiante.Nombre, Correo = estudiante.Correo, CursoId = estudiante.CursoId, Curso = cursoNombre };
    }

    public async Task MatricularAsync(int estudianteId, int asignaturaId, CancellationToken cancellationToken = default)
    {
        var registro = await context.EstudianteAsignaturas
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.EstudianteId == estudianteId && x.AsignaturaId == asignaturaId, cancellationToken);

        if (registro is null)
        {
            context.EstudianteAsignaturas.Add(new EstudianteAsignatura { EstudianteId = estudianteId, AsignaturaId = asignaturaId });
        }
        else
        {
            registro.IsDeleted = false;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DesmatricularAsync(int estudianteId, int asignaturaId, CancellationToken cancellationToken = default)
    {
        var registro = await context.EstudianteAsignaturas
            .FirstOrDefaultAsync(ea => ea.EstudianteId == estudianteId && ea.AsignaturaId == asignaturaId, cancellationToken);
        if (registro is not null)
        {
            context.EstudianteAsignaturas.Remove(registro);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<EstudianteListItemDto?> UpdateAsync(int id, string nombre, string correo, int cursoId, string? hash, CancellationToken cancellationToken = default)
    {
        var estudiante = await context.Estudiantes.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (estudiante is null) return null;
        estudiante.Nombre = nombre;
        estudiante.Correo = correo;
        estudiante.CursoId = cursoId;
        if (hash is not null) estudiante.Contrasena = hash;
        await context.SaveChangesAsync();
        var cursoNombre = await GetCursoNombreAsync(cursoId);
        return new EstudianteListItemDto { Id = estudiante.Id, Nombre = estudiante.Nombre, Correo = estudiante.Correo, CursoId = estudiante.CursoId, Curso = cursoNombre };
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var matriculas = await context.EstudianteAsignaturas.Where(ea => ea.EstudianteId == id).ToListAsync(cancellationToken);
        context.EstudianteAsignaturas.RemoveRange(matriculas);

        var notas = await context.Notas.Where(n => n.EstudianteId == id).ToListAsync(cancellationToken);
        context.Notas.RemoveRange(notas);

        var tokens = await context.RefreshTokens
            .Where(t => t.UserId == id && t.Rol == "alumno")
            .ToListAsync(cancellationToken);
        context.RefreshTokens.RemoveRange(tokens);

        var estudiante = await context.Estudiantes.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (estudiante is not null) context.Estudiantes.Remove(estudiante);
        await context.SaveChangesAsync(cancellationToken);
    }

    #endregion
}
