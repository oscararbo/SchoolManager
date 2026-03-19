using Back.Api.Data;
using Back.Api.Dtos;
using Back.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Back.Api.Services;

public class EstudiantesService(AppDbContext context, IPasswordService passwordService) : IEstudiantesService
{
    public async Task<IActionResult> GetAllAsync()
    {
        var estudiantes = await context.Estudiantes
            .AsNoTracking()
            .Select(e => new EstudianteListItemDto
            {
                Id = e.Id,
                Nombre = e.Nombre,
                Correo = e.Correo,
                CursoId = e.CursoId,
                Curso = e.Curso != null ? e.Curso.Nombre : null
            })
            .ToListAsync();

        return new OkObjectResult(estudiantes);
    }

    public async Task<IActionResult> GetByIdAsync(int id)
    {
        var estudiante = await context.Estudiantes
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

        return estudiante is null ? new NotFoundResult() : new OkObjectResult(estudiante);
    }

    public async Task<IActionResult> CreateAsync(CreateEstudianteDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            return new BadRequestObjectResult("El nombre del estudiante es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(dto.Correo))
        {
            return new BadRequestObjectResult("El correo del estudiante es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(dto.Contrasena))
        {
            return new BadRequestObjectResult("La contrasena del estudiante es obligatoria.");
        }

        if (dto.CursoId <= 0)
        {
            return new BadRequestObjectResult("El curso del estudiante es obligatorio.");
        }

        var correo = dto.Correo.Trim().ToLowerInvariant();

        var correoDuplicado = await context.Estudiantes
            .AnyAsync(e => e.Correo.ToLower() == correo);

        if (correoDuplicado)
        {
            return new BadRequestObjectResult("Ya existe un estudiante con ese correo.");
        }

        var curso = await context.Cursos
            .AsNoTracking()
            .Where(c => c.Id == dto.CursoId)
            .Select(c => new { c.Id, c.Nombre })
            .FirstOrDefaultAsync();

        if (curso is null)
        {
            return new BadRequestObjectResult("El curso indicado no existe.");
        }

        var estudiante = new Estudiante
        {
            Nombre = dto.Nombre.Trim(),
            Correo = correo,
            Contrasena = passwordService.Hash(dto.Contrasena.Trim()),
            CursoId = dto.CursoId
        };

        context.Estudiantes.Add(estudiante);
        await context.SaveChangesAsync();

        return new CreatedResult($"/api/estudiantes/{estudiante.Id}", new EstudianteListItemDto
        {
            Id = estudiante.Id,
            Nombre = estudiante.Nombre,
            Correo = estudiante.Correo,
            CursoId = estudiante.CursoId,
            Curso = curso.Nombre
        });
    }

    public async Task<IActionResult> MatricularAsync(int id, int asignaturaId)
    {
        var estudiante = await context.Estudiantes.FindAsync(id);
        if (estudiante is null)
        {
            return new NotFoundObjectResult("El estudiante no existe.");
        }

        var asignatura = await context.Asignaturas.FirstOrDefaultAsync(a => a.Id == asignaturaId);
        if (asignatura is null)
        {
            return new NotFoundObjectResult("La asignatura no existe.");
        }

        if (asignatura.CursoId != estudiante.CursoId)
        {
            return new BadRequestObjectResult("El estudiante solo puede matricularse en asignaturas de su curso.");
        }

        var yaMatriculado = await context.EstudianteAsignaturas
            .AnyAsync(x => x.EstudianteId == id && x.AsignaturaId == asignaturaId);
        if (yaMatriculado)
        {
            return new BadRequestObjectResult("El estudiante ya esta matriculado en esta asignatura.");
        }

        context.EstudianteAsignaturas.Add(new EstudianteAsignatura
        {
            EstudianteId = id,
            AsignaturaId = asignaturaId
        });

        await context.SaveChangesAsync();
        return new OkResult();
    }

    public async Task<IActionResult> GetPanelAlumnoAsync(int id, ClaimsPrincipal user)
    {
        if (!UsuarioCoincideConEstudiante(id, user))
        {
            return new ForbidResult();
        }

        var estudiante = await context.Estudiantes
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new { e.Id, e.Nombre, e.CursoId, Curso = e.Curso!.Nombre })
            .FirstOrDefaultAsync();

        if (estudiante is null)
        {
            return new NotFoundObjectResult("El estudiante no existe.");
        }

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
                    a.Id,
                    a.Nombre,
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
                .OrderBy(t => t.Trimestre)
                .ThenBy(t => t.Nombre)
                .ToListAsync();

            var tareaIds = tareas.Select(t => t.Id).ToList();
            var notasAlumno = await context.Notas
                .AsNoTracking()
                .Where(n => n.EstudianteId == id && tareaIds.Contains(n.TareaId))
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
                var vals = notasList.Where(n => n.Trimestre == trim && n.Valor.HasValue).Select(n => n.Valor!.Value).ToList();
                return vals.Count > 0 ? Math.Round(vals.Average(), 2) : null;
            }

            var t1 = Media(1);
            var t2 = Media(2);
            var t3 = Media(3);
            decimal? notaFinal = (t1.HasValue && t2.HasValue && t3.HasValue) ? Math.Round((t1.Value + t2.Value + t3.Value) / 3, 2) : null;

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

        return new OkObjectResult(new AlumnoPanelDto
        {
            Id = estudiante.Id,
            Nombre = estudiante.Nombre,
            Curso = new AlumnoCursoDto { CursoId = estudiante.CursoId, Curso = estudiante.Curso },
            Materias = materias.OrderBy(m => m.Asignatura).ToList()
        });
    }

    private static bool UsuarioCoincideConEstudiante(int estudianteId, ClaimsPrincipal user)
    {
        if (user.IsInRole("admin"))
        {
            return true;
        }

        var idClaim = user.FindFirstValue("id") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idClaim, out var usuarioId) && usuarioId == estudianteId;
    }

    public async Task<IActionResult> UpdateAsync(int id, UpdateEstudianteDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return new BadRequestObjectResult("El nombre del estudiante es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Correo))
            return new BadRequestObjectResult("El correo del estudiante es obligatorio.");
        if (dto.CursoId <= 0)
            return new BadRequestObjectResult("El curso del estudiante es obligatorio.");

        var estudiante = await context.Estudiantes.FindAsync(id);
        if (estudiante is null)
            return new NotFoundObjectResult("El estudiante no existe.");

        var correo = dto.Correo.Trim().ToLowerInvariant();
        var correoUsado = await context.Estudiantes.AnyAsync(e => e.Correo.ToLower() == correo && e.Id != id);
        if (correoUsado)
            return new BadRequestObjectResult("Ya existe otro estudiante con ese correo.");

        var cursoExiste = await context.Cursos.AnyAsync(c => c.Id == dto.CursoId);
        if (!cursoExiste)
            return new BadRequestObjectResult("El curso indicado no existe.");

        estudiante.Nombre = dto.Nombre.Trim();
        estudiante.Correo = correo;
        estudiante.CursoId = dto.CursoId;
        if (!string.IsNullOrWhiteSpace(dto.NuevaContrasena))
            estudiante.Contrasena = passwordService.Hash(dto.NuevaContrasena.Trim());

        await context.SaveChangesAsync();

        var cursoNombre = (await context.Cursos.AsNoTracking().Where(c => c.Id == dto.CursoId)
            .Select(c => c.Nombre).FirstOrDefaultAsync())!;

        return new OkObjectResult(new EstudianteListItemDto
        {
            Id = estudiante.Id,
            Nombre = estudiante.Nombre,
            Correo = estudiante.Correo,
            CursoId = estudiante.CursoId,
            Curso = cursoNombre
        });
    }

    public async Task<IActionResult> DeleteAsync(int id)
    {
        var estudiante = await context.Estudiantes.FindAsync(id);
        if (estudiante is null)
            return new NotFoundObjectResult("El estudiante no existe.");

        var matriculas = await context.EstudianteAsignaturas.Where(ea => ea.EstudianteId == id).ToListAsync();
        context.EstudianteAsignaturas.RemoveRange(matriculas);

        var notas = await context.Notas.Where(n => n.EstudianteId == id).ToListAsync();
        context.Notas.RemoveRange(notas);

        context.Estudiantes.Remove(estudiante);
        await context.SaveChangesAsync();
        return new NoContentResult();
    }
}
