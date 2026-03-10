using Back.Api.Data;
using Back.Api.Dtos;
using Back.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Back.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CursosController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var cursosBase = await context.Cursos
            .AsNoTracking()
            .Select(c => new { c.Id, c.Nombre })
            .OrderBy(c => c.Nombre)
            .ToListAsync();

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
            .ToListAsync();

        var cursos = cursosBase.Select(c => new
        {
            c.Id,
            c.Nombre,
            Asignaturas = asignaturasConProfesor
                .Where(a => a.CursoId == c.Id)
                .Select(a => new
                {
                    a.Id,
                    a.Nombre,
                    a.ProfesorId,
                    a.Profesor
                })
                .ToList()
        });

        return Ok(cursos);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var curso = await context.Cursos
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new { c.Id, c.Nombre })
            .FirstOrDefaultAsync();

        if (curso is null)
        {
            return NotFound("El curso no existe.");
        }

        var alumnos = await context.Estudiantes
            .AsNoTracking()
            .Where(e => e.CursoId == id)
            .Select(e => new { e.Id, e.Nombre })
            .OrderBy(e => e.Nombre)
            .ToListAsync();

        var asignaturas = await context.Asignaturas
            .AsNoTracking()
            .Where(a => a.CursoId == id)
            .Select(a => new
            {
                a.Id,
                a.Nombre,
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

        return Ok(new
        {
            curso.Id,
            curso.Nombre,
            Alumnos = alumnos,
            Asignaturas = asignaturas
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCursoDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            return BadRequest("El nombre del curso es obligatorio.");
        }

        var curso = new Curso { Nombre = dto.Nombre.Trim() };
        context.Cursos.Add(curso);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = curso.Id }, curso);
    }
}
