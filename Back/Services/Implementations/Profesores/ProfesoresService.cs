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

        return new CreatedResult($"/api/profesores/{profesor.Id}", new ProfesorDetalleDto
        {
            Id = profesor.Id,
            Nombre = profesor.Nombre,
            Correo = profesor.Correo,
            EsAdmin = profesor.EsAdmin,
            Cursos = new()
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

        var alumnos = await context.EstudianteAsignaturas
            .AsNoTracking()
            .Where(ea => ea.AsignaturaId == asignaturaId)
            .Select(ea => new AsignaturaAlumnoDto
            {
                EstudianteId = ea.EstudianteId,
                Alumno = ea.Estudiante!.Nombre,
                Nota = context.Notas
                    .Where(n => n.EstudianteId == ea.EstudianteId && n.AsignaturaId == asignaturaId)
                    .Select(n => (decimal?)n.Valor)
                    .FirstOrDefault()
            })
            .OrderBy(x => x.Alumno)
            .ToListAsync();

        return new OkObjectResult(new AsignaturaAlumnosResponseDto
        {
            Asignatura = asignaturaInfo,
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

        var profesorExiste = await context.Profesores.AnyAsync(p => p.Id == profesorId);
        if (!profesorExiste)
        {
            return new NotFoundObjectResult("El profesor no existe.");
        }

        var estudiante = await context.Estudiantes.FirstOrDefaultAsync(e => e.Id == dto.EstudianteId);
        if (estudiante is null)
        {
            return new NotFoundObjectResult("El estudiante no existe.");
        }

        var estudianteMatriculado = await context.EstudianteAsignaturas.AnyAsync(x =>
            x.EstudianteId == dto.EstudianteId && x.AsignaturaId == dto.AsignaturaId);

        if (!estudianteMatriculado)
        {
            return new BadRequestObjectResult("El estudiante no esta matriculado en esa asignatura.");
        }

        var profesorImparte = await context.ProfesorAsignaturaCursos.AnyAsync(x =>
            x.ProfesorId == profesorId &&
            x.AsignaturaId == dto.AsignaturaId &&
            x.CursoId == estudiante.CursoId);

        if (!profesorImparte)
        {
            return new BadRequestObjectResult("El profesor no imparte esa asignatura al curso del estudiante.");
        }

        var notaExistente = await context.Notas
            .FirstOrDefaultAsync(n => n.EstudianteId == dto.EstudianteId && n.AsignaturaId == dto.AsignaturaId);

        if (notaExistente is null)
        {
            context.Notas.Add(new Nota
            {
                EstudianteId = dto.EstudianteId,
                AsignaturaId = dto.AsignaturaId,
                ProfesorId = profesorId,
                Valor = dto.Valor
            });
        }
        else
        {
            notaExistente.ProfesorId = profesorId;
            notaExistente.Valor = dto.Valor;
        }

        await context.SaveChangesAsync();
        return new OkResult();
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
}
