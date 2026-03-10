using Back.Api.Data;
using Back.Api.Dtos;
using Back.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Back.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfesoresController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var profesores = await context.Profesores
            .AsNoTracking()
            .Select(p => new
            {
                p.Id,
                p.Nombre,
                Imparticiones = p.ProfesorAsignaturaCursos.Select(i => new
                {
                    i.AsignaturaId,
                    Asignatura = i.Asignatura!.Nombre,
                    i.CursoId,
                    Curso = i.Curso!.Nombre
                })
            })
            .ToListAsync();

        return Ok(profesores);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var profesor = await context.Profesores
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new { p.Id, p.Nombre })
            .FirstOrDefaultAsync();

        if (profesor is null)
        {
            return NotFound("El profesor no existe.");
        }

        var imparticiones = await context.ProfesorAsignaturaCursos
            .AsNoTracking()
            .Where(i => i.ProfesorId == id)
            .Select(i => new
            {
                i.CursoId,
                Curso = i.Curso!.Nombre,
                i.AsignaturaId,
                Asignatura = i.Asignatura!.Nombre
            })
            .OrderBy(i => i.Curso)
            .ThenBy(i => i.Asignatura)
            .ToListAsync();

        var cursos = imparticiones
            .GroupBy(i => new { i.CursoId, i.Curso })
            .Select(g => new
            {
                g.Key.CursoId,
                Curso = g.Key.Curso,
                Asignaturas = g.Select(x => new
                {
                    x.AsignaturaId,
                    Nombre = x.Asignatura
                })
            })
            .OrderBy(x => x.Curso)
            .ToList();

        return Ok(new
        {
            profesor.Id,
            profesor.Nombre,
            Cursos = cursos
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateProfesorDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            return BadRequest("El nombre del profesor es obligatorio.");
        }

        var profesor = new Profesor { Nombre = dto.Nombre.Trim() };
        context.Profesores.Add(profesor);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { id = profesor.Id }, profesor);
    }

    [HttpPost("{profesorId:int}/imparticiones")]
    public async Task<IActionResult> AsignarImparticion(int profesorId, AsignarImparticionDto dto)
    {
        var profesorExiste = await context.Profesores.AnyAsync(p => p.Id == profesorId);
        if (!profesorExiste)
        {
            return NotFound("El profesor no existe.");
        }

        var asignatura = await context.Asignaturas.FirstOrDefaultAsync(a => a.Id == dto.AsignaturaId);
        if (asignatura is null)
        {
            return NotFound("La asignatura no existe.");
        }

        var cursoExiste = await context.Cursos.AnyAsync(c => c.Id == dto.CursoId);
        if (!cursoExiste)
        {
            return NotFound("El curso no existe.");
        }

        if (asignatura.CursoId != dto.CursoId)
        {
            return BadRequest("La asignatura no pertenece a ese curso.");
        }

        var asignaturaYaTieneProfesor = await context.ProfesorAsignaturaCursos
            .AnyAsync(x => x.AsignaturaId == dto.AsignaturaId && x.ProfesorId != profesorId);
        if (asignaturaYaTieneProfesor)
        {
            return BadRequest("La asignatura ya tiene un profesor asignado.");
        }

        var yaAsignado = await context.ProfesorAsignaturaCursos.AnyAsync(x =>
            x.ProfesorId == profesorId &&
            x.AsignaturaId == dto.AsignaturaId &&
            x.CursoId == dto.CursoId);

        if (yaAsignado)
        {
            return BadRequest("La imparticion ya existe para ese profesor, asignatura y curso.");
        }

        context.ProfesorAsignaturaCursos.Add(new ProfesorAsignaturaCurso
        {
            ProfesorId = profesorId,
            AsignaturaId = dto.AsignaturaId,
            CursoId = dto.CursoId
        });

        await context.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("{profesorId:int}/notas")]
    public async Task<IActionResult> PonerNota(int profesorId, PonerNotaDto dto)
    {
        if (dto.Valor < 0 || dto.Valor > 10)
        {
            return BadRequest("La nota debe estar entre 0 y 10.");
        }

        var profesorExiste = await context.Profesores.AnyAsync(p => p.Id == profesorId);
        if (!profesorExiste)
        {
            return NotFound("El profesor no existe.");
        }

        var estudiante = await context.Estudiantes.FirstOrDefaultAsync(e => e.Id == dto.EstudianteId);
        if (estudiante is null)
        {
            return NotFound("El estudiante no existe.");
        }

        var estudianteMatriculado = await context.EstudianteAsignaturas.AnyAsync(x =>
            x.EstudianteId == dto.EstudianteId && x.AsignaturaId == dto.AsignaturaId);

        if (!estudianteMatriculado)
        {
            return BadRequest("El estudiante no esta matriculado en esa asignatura.");
        }

        var profesorImparte = await context.ProfesorAsignaturaCursos.AnyAsync(x =>
            x.ProfesorId == profesorId &&
            x.AsignaturaId == dto.AsignaturaId &&
            x.CursoId == estudiante.CursoId);

        if (!profesorImparte)
        {
            return BadRequest("El profesor no imparte esa asignatura al curso del estudiante.");
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
        return Ok();
    }
}
