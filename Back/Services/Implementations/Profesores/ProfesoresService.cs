using Back.Api.Data;
using Back.Api.Dtos;
using Back.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Back.Api.Services;

public class ProfesoresService(AppDbContext context, IPasswordService passwordService) : IProfesoresService
{
    public async Task<IActionResult> GetAllAsync()
    {
        var profesores = await context.Profesores
            .AsNoTracking()
            .Select(p => new ProfesorListItemDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Correo = p.Correo,
                EsAdmin = p.EsAdmin,
                Imparticiones = p.ProfesorAsignaturaCursos.Select(i => new ProfesorImparticionDto
                {
                    AsignaturaId = i.AsignaturaId,
                    Asignatura = i.Asignatura!.Nombre,
                    CursoId = i.CursoId,
                    Curso = i.Curso!.Nombre
                }).ToList()
            })
            .ToListAsync();

        return new OkObjectResult(profesores);
    }

    public async Task<IActionResult> GetByIdAsync(int id)
    {
        var profesor = await context.Profesores
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProfesorDetalleDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Correo = p.Correo,
                EsAdmin = p.EsAdmin
            })
            .FirstOrDefaultAsync();

        if (profesor is null)
        {
            return new NotFoundObjectResult("El profesor no existe.");
        }

        var imparticiones = await context.ProfesorAsignaturaCursos
            .AsNoTracking()
            .Where(i => i.ProfesorId == id)
            .Select(i => new ProfesorImparticionDto
            {
                CursoId = i.CursoId,
                Curso = i.Curso!.Nombre,
                AsignaturaId = i.AsignaturaId,
                Asignatura = i.Asignatura!.Nombre
            })
            .OrderBy(i => i.Curso)
            .ThenBy(i => i.Asignatura)
            .ToListAsync();

        var cursos = imparticiones
            .GroupBy(i => new { i.CursoId, i.Curso })
            .Select(g => new ProfesorCursoPanelDto
            {
                CursoId = g.Key.CursoId,
                Curso = g.Key.Curso,
                Asignaturas = g.Select(x => new ProfesorCursoAsignaturaDto
                {
                    AsignaturaId = x.AsignaturaId,
                    Nombre = x.Asignatura
                }).ToList()
            })
            .OrderBy(x => x.Curso)
            .ToList();

        profesor.Cursos = cursos;
        return new OkObjectResult(profesor);
    }

    public async Task<IActionResult> CreateAsync(CreateProfesorDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            return new BadRequestObjectResult("El nombre del profesor es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(dto.Correo))
        {
            return new BadRequestObjectResult("El correo del profesor es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(dto.Contrasena))
        {
            return new BadRequestObjectResult("La contrasena del profesor es obligatoria.");
        }

        var correo = dto.Correo.Trim().ToLowerInvariant();

        var correoDuplicado = await context.Profesores
            .AnyAsync(p => p.Correo.ToLower() == correo);

        if (correoDuplicado)
        {
            return new BadRequestObjectResult("Ya existe un profesor con ese correo.");
        }

        var profesor = new Profesor
        {
            Nombre = dto.Nombre.Trim(),
            Correo = correo,
            Contrasena = passwordService.Hash(dto.Contrasena.Trim()),
            EsAdmin = dto.EsAdmin
        };
        context.Profesores.Add(profesor);
        await context.SaveChangesAsync();

        return new CreatedResult($"/api/profesores/{profesor.Id}", new ProfesorListItemDto
        {
            Id = profesor.Id,
            Nombre = profesor.Nombre,
            Correo = profesor.Correo,
            EsAdmin = profesor.EsAdmin,
            Imparticiones = new()
        });
    }

    public async Task<IActionResult> GetPanelProfesorAsync(int id, ClaimsPrincipal user)
    {
        if (!UsuarioCoincideConProfesor(id, user))
        {
            return new ForbidResult();
        }

        var profesor = await context.Profesores
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProfesorPanelDto
            {
                Id = p.Id,
                Nombre = p.Nombre
            })
            .FirstOrDefaultAsync();

        if (profesor is null)
        {
            return new NotFoundObjectResult("El profesor no existe.");
        }

        var imparticiones = await context.ProfesorAsignaturaCursos
            .AsNoTracking()
            .Where(i => i.ProfesorId == id)
            .Select(i => new ProfesorImparticionDto
            {
                CursoId = i.CursoId,
                Curso = i.Curso!.Nombre,
                AsignaturaId = i.AsignaturaId,
                Asignatura = i.Asignatura!.Nombre
            })
            .OrderBy(i => i.Curso)
            .ThenBy(i => i.Asignatura)
            .ToListAsync();

        var cursos = imparticiones
            .GroupBy(i => new { i.CursoId, i.Curso })
            .Select(g => new ProfesorCursoPanelDto
            {
                CursoId = g.Key.CursoId,
                Curso = g.Key.Curso,
                Asignaturas = g.Select(x => new ProfesorCursoAsignaturaDto
                {
                    AsignaturaId = x.AsignaturaId,
                    Nombre = x.Asignatura
                }).ToList()
            })
            .OrderBy(x => x.Curso)
            .ToList();

        profesor.Cursos = cursos;
        return new OkObjectResult(profesor);
    }

    public async Task<IActionResult> GetAlumnosDeAsignaturaAsync(int profesorId, int asignaturaId, ClaimsPrincipal user)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
        {
            return new ForbidResult();
        }

        var profesorExiste = await context.Profesores.AnyAsync(p => p.Id == profesorId);
        if (!profesorExiste)
        {
            return new NotFoundObjectResult("El profesor no existe.");
        }

        var profesorImparteAsignatura = await context.ProfesorAsignaturaCursos
            .AnyAsync(pac => pac.ProfesorId == profesorId && pac.AsignaturaId == asignaturaId);

        if (!profesorImparteAsignatura)
        {
            return new BadRequestObjectResult("El profesor no imparte esta asignatura.");
        }

        var asignaturaInfo = await context.Asignaturas
            .AsNoTracking()
            .Where(a => a.Id == asignaturaId)
            .Select(a => new AsignaturaInfoDto
            {
                Id = a.Id,
                Nombre = a.Nombre,
                CursoId = a.CursoId,
                Curso = a.Curso!.Nombre
            })
            .FirstOrDefaultAsync();

        if (asignaturaInfo is null)
        {
            return new NotFoundObjectResult("La asignatura no existe.");
        }

        var tareas = await context.Tareas
            .AsNoTracking()
            .Where(t => t.AsignaturaId == asignaturaId)
            .OrderBy(t => t.Trimestre)
            .ThenBy(t => t.Nombre)
            .Select(t => new TareaResumenDto { TareaId = t.Id, Nombre = t.Nombre, Trimestre = t.Trimestre })
            .ToListAsync();

        var tareaIds = tareas.Select(t => t.TareaId).ToList();

        var alumnosRaw = await context.EstudianteAsignaturas
            .AsNoTracking()
            .Where(ea => ea.AsignaturaId == asignaturaId)
            .Select(ea => new { ea.EstudianteId, Alumno = ea.Estudiante!.Nombre })
            .OrderBy(a => a.Alumno)
            .ToListAsync();

        var alumnoIds = alumnosRaw.Select(a => a.EstudianteId).ToList();

        var todasNotas = await context.Notas
            .AsNoTracking()
            .Where(n => alumnoIds.Contains(n.EstudianteId) && tareaIds.Contains(n.TareaId))
            .ToListAsync();

        var alumnos = alumnosRaw.Select(a =>
        {
            var notasAlumno = todasNotas.Where(n => n.EstudianteId == a.EstudianteId).ToList();
            var notasList = tareas.Select(t =>
            {
                var nota = notasAlumno.FirstOrDefault(n => n.TareaId == t.TareaId);
                return new AsignaturaNotaAlumnoDto { TareaId = t.TareaId, Valor = nota?.Valor };
            }).ToList();

            decimal? Media(int trim)
            {
                var vals = tareas.Where(t => t.Trimestre == trim)
                    .Select(t => notasList.First(n => n.TareaId == t.TareaId).Valor)
                    .Where(v => v.HasValue).Select(v => v!.Value).ToList();
                return vals.Count > 0 ? vals.Average() : null;
            }

            var t1 = Media(1);
            var t2 = Media(2);
            var t3 = Media(3);
            decimal? notaFinal = (t1.HasValue && t2.HasValue && t3.HasValue) ? Math.Round((t1.Value + t2.Value + t3.Value) / 3, 2) : null;

            return new AsignaturaAlumnoDto
            {
                EstudianteId = a.EstudianteId,
                Alumno = a.Alumno,
                Notas = notasList,
                Medias = new MediasTrimestralesDto { T1 = t1.HasValue ? Math.Round(t1.Value, 2) : null, T2 = t2.HasValue ? Math.Round(t2.Value, 2) : null, T3 = t3.HasValue ? Math.Round(t3.Value, 2) : null },
                NotaFinal = notaFinal
            };
        }).ToList();

        return new OkObjectResult(new AsignaturaAlumnosResponseDto
        {
            Asignatura = asignaturaInfo,
            Tareas = tareas,
            Alumnos = alumnos
        });
    }

    public async Task<IActionResult> AsignarImparticionAsync(int profesorId, AsignarImparticionDto dto, ClaimsPrincipal user)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
        {
            return new ForbidResult();
        }

        var profesorExiste = await context.Profesores.AnyAsync(p => p.Id == profesorId);
        if (!profesorExiste)
        {
            return new NotFoundObjectResult("El profesor no existe.");
        }

        var asignatura = await context.Asignaturas.FirstOrDefaultAsync(a => a.Id == dto.AsignaturaId);
        if (asignatura is null)
        {
            return new NotFoundObjectResult("La asignatura no existe.");
        }

        var cursoExiste = await context.Cursos.AnyAsync(c => c.Id == dto.CursoId);
        if (!cursoExiste)
        {
            return new NotFoundObjectResult("El curso no existe.");
        }

        if (asignatura.CursoId != dto.CursoId)
        {
            return new BadRequestObjectResult("La asignatura no pertenece a ese curso.");
        }

        var asignaturaYaTieneProfesor = await context.ProfesorAsignaturaCursos
            .AnyAsync(x => x.AsignaturaId == dto.AsignaturaId && x.ProfesorId != profesorId);
        if (asignaturaYaTieneProfesor)
        {
            return new BadRequestObjectResult("La asignatura ya tiene un profesor asignado.");
        }

        var yaAsignado = await context.ProfesorAsignaturaCursos.AnyAsync(x =>
            x.ProfesorId == profesorId &&
            x.AsignaturaId == dto.AsignaturaId &&
            x.CursoId == dto.CursoId);

        if (yaAsignado)
        {
            return new BadRequestObjectResult("La imparticion ya existe para ese profesor, asignatura y curso.");
        }

        context.ProfesorAsignaturaCursos.Add(new ProfesorAsignaturaCurso
        {
            ProfesorId = profesorId,
            AsignaturaId = dto.AsignaturaId,
            CursoId = dto.CursoId
        });

        await context.SaveChangesAsync();
        return new OkResult();
    }

    public async Task<IActionResult> PonerNotaAsync(int profesorId, PonerNotaDto dto, ClaimsPrincipal user)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
        {
            return new ForbidResult();
        }

        if (dto.Valor < 0 || dto.Valor > 10)
        {
            return new BadRequestObjectResult("La nota debe estar entre 0 y 10.");
        }

        var tarea = await context.Tareas.FirstOrDefaultAsync(t => t.Id == dto.TareaId);
        if (tarea is null)
        {
            return new NotFoundObjectResult("La tarea no existe.");
        }

        if (tarea.ProfesorId != profesorId && !user.IsInRole("admin"))
        {
            return new ForbidResult();
        }

        var estudiante = await context.Estudiantes.FirstOrDefaultAsync(e => e.Id == dto.EstudianteId);
        if (estudiante is null)
        {
            return new NotFoundObjectResult("El estudiante no existe.");
        }

        var estudianteMatriculado = await context.EstudianteAsignaturas.AnyAsync(x =>
            x.EstudianteId == dto.EstudianteId && x.AsignaturaId == tarea.AsignaturaId);
        if (!estudianteMatriculado)
        {
            return new BadRequestObjectResult("El estudiante no esta matriculado en esa asignatura.");
        }

        var profesorImparte = await context.ProfesorAsignaturaCursos.AnyAsync(x =>
            x.ProfesorId == tarea.ProfesorId && x.AsignaturaId == tarea.AsignaturaId && x.CursoId == estudiante.CursoId);
        if (!profesorImparte)
        {
            return new BadRequestObjectResult("El profesor no imparte esa asignatura al curso del estudiante.");
        }

        var notaExistente = await context.Notas.FirstOrDefaultAsync(n =>
            n.EstudianteId == dto.EstudianteId && n.TareaId == dto.TareaId);

        if (notaExistente is null)
        {
            context.Notas.Add(new Nota
            {
                EstudianteId = dto.EstudianteId,
                TareaId = dto.TareaId,
                Valor = dto.Valor
            });
        }
        else
        {
            notaExistente.Valor = dto.Valor;
        }

        await context.SaveChangesAsync();
        return new OkResult();
    }

    public async Task<IActionResult> CrearTareaAsync(int profesorId, CreateTareaDto dto, ClaimsPrincipal user)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
        {
            return new ForbidResult();
        }

        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            return new BadRequestObjectResult("El nombre de la tarea es obligatorio.");
        }

        if (dto.Trimestre < 1 || dto.Trimestre > 3)
        {
            return new BadRequestObjectResult("El trimestre debe ser 1, 2 o 3.");
        }

        var profesorExiste = await context.Profesores.AnyAsync(p => p.Id == profesorId);
        if (!profesorExiste)
        {
            return new NotFoundObjectResult("El profesor no existe.");
        }

        var profesorImparteAsignatura = await context.ProfesorAsignaturaCursos
            .AnyAsync(pac => pac.ProfesorId == profesorId && pac.AsignaturaId == dto.AsignaturaId);
        if (!profesorImparteAsignatura)
        {
            return new BadRequestObjectResult("El profesor no imparte esa asignatura.");
        }

        var asignaturaNombre = await context.Asignaturas
            .Where(a => a.Id == dto.AsignaturaId)
            .Select(a => a.Nombre)
            .FirstOrDefaultAsync();

        if (asignaturaNombre is null)
        {
            return new NotFoundObjectResult("La asignatura no existe.");
        }

        var tarea = new Tarea
        {
            Nombre = dto.Nombre.Trim(),
            Trimestre = dto.Trimestre,
            AsignaturaId = dto.AsignaturaId,
            ProfesorId = profesorId
        };

        context.Tareas.Add(tarea);
        await context.SaveChangesAsync();

        return new CreatedResult($"/api/profesores/{profesorId}/tareas/{tarea.Id}", new TareaDetalleDto
        {
            Id = tarea.Id,
            Nombre = tarea.Nombre,
            Trimestre = tarea.Trimestre,
            AsignaturaId = tarea.AsignaturaId,
            Asignatura = asignaturaNombre
        });
    }

    public async Task<IActionResult> GetTareasDeAsignaturaAsync(int profesorId, int asignaturaId, ClaimsPrincipal user)
    {
        if (!UsuarioCoincideConProfesor(profesorId, user))
        {
            return new ForbidResult();
        }

        var tareas = await context.Tareas
            .AsNoTracking()
            .Where(t => t.AsignaturaId == asignaturaId && t.ProfesorId == profesorId)
            .OrderBy(t => t.Trimestre)
            .ThenBy(t => t.Nombre)
            .Select(t => new TareaResumenDto { TareaId = t.Id, Nombre = t.Nombre, Trimestre = t.Trimestre })
            .ToListAsync();

        return new OkObjectResult(tareas);
    }

    private static bool UsuarioCoincideConProfesor(int profesorId, ClaimsPrincipal user)
    {
        if (user.IsInRole("admin"))
        {
            return true;
        }

        var idClaim = user.FindFirstValue("id") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idClaim, out var usuarioId) && usuarioId == profesorId;
    }

    public async Task<IActionResult> UpdateAsync(int id, UpdateProfesorDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return new BadRequestObjectResult("El nombre del profesor es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Correo))
            return new BadRequestObjectResult("El correo del profesor es obligatorio.");

        var profesor = await context.Profesores.FindAsync(id);
        if (profesor is null)
            return new NotFoundObjectResult("El profesor no existe.");

        var correo = dto.Correo.Trim().ToLowerInvariant();
        var correoUsado = await context.Profesores.AnyAsync(p => p.Correo.ToLower() == correo && p.Id != id);
        if (correoUsado)
            return new BadRequestObjectResult("Ya existe otro profesor con ese correo.");

        profesor.Nombre = dto.Nombre.Trim();
        profesor.Correo = correo;
        profesor.EsAdmin = dto.EsAdmin;
        if (!string.IsNullOrWhiteSpace(dto.NuevaContrasena))
            profesor.Contrasena = passwordService.Hash(dto.NuevaContrasena.Trim());

        await context.SaveChangesAsync();

        return new OkObjectResult(new ProfesorListItemDto
        {
            Id = profesor.Id,
            Nombre = profesor.Nombre,
            Correo = profesor.Correo,
            EsAdmin = profesor.EsAdmin,
            Imparticiones = await context.ProfesorAsignaturaCursos
                .AsNoTracking()
                .Where(i => i.ProfesorId == id)
                .Select(i => new ProfesorImparticionDto
                {
                    AsignaturaId = i.AsignaturaId,
                    Asignatura = i.Asignatura!.Nombre,
                    CursoId = i.CursoId,
                    Curso = i.Curso!.Nombre
                }).ToListAsync()
        });
    }

    public async Task<IActionResult> DeleteAsync(int id)
    {
        var profesor = await context.Profesores.FindAsync(id);
        if (profesor is null)
            return new NotFoundObjectResult("El profesor no existe.");

        var imparticiones = await context.ProfesorAsignaturaCursos.Where(i => i.ProfesorId == id).ToListAsync();
        context.ProfesorAsignaturaCursos.RemoveRange(imparticiones);

        var tareaIds = await context.Tareas.Where(t => t.ProfesorId == id).Select(t => t.Id).ToListAsync();
        if (tareaIds.Count > 0)
        {
            var notas = await context.Notas.Where(n => tareaIds.Contains(n.TareaId)).ToListAsync();
            context.Notas.RemoveRange(notas);
            var tareas = await context.Tareas.Where(t => t.ProfesorId == id).ToListAsync();
            context.Tareas.RemoveRange(tareas);
        }

        context.Profesores.Remove(profesor);
        await context.SaveChangesAsync();
        return new NoContentResult();
    }
}
