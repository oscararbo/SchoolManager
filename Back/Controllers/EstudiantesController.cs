using Back.Api.Data;
using Back.Api.Dtos;
using Back.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Back.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EstudiantesController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var estudiantes = await context.Estudiantes
            .AsNoTracking()
            .Select(e => new
            {
                e.Id,
                e.Nombre,
                e.CursoId,
                Curso = e.Curso != null ? e.Curso.Nombre : null
            })
            .ToListAsync();

        return Ok(estudiantes);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var estudiante = await context.Estudiantes
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new
            {
                e.Id,
                e.Nombre,
                e.CursoId,
                Curso = e.Curso != null ? e.Curso.Nombre : null,
                Asignaturas = e.EstudianteAsignaturas.Select(x => new
                {
                    x.AsignaturaId,
                    Nombre = x.Asignatura!.Nombre,
                    ProfesorId = x.Asignatura.ProfesorAsignaturaCursos
                        .Where(pac => pac.CursoId == e.CursoId)
                        .Select(pac => (int?)pac.ProfesorId)
                        .FirstOrDefault(),
                    Profesor = x.Asignatura.ProfesorAsignaturaCursos
                        .Where(pac => pac.CursoId == e.CursoId)
                        .Select(pac => pac.Profesor!.Nombre)
                        .FirstOrDefault()
                }),
                Notas = e.Notas.Select(n => new
                {
                    n.AsignaturaId,
                    Asignatura = n.Asignatura!.Nombre,
                    n.Valor,
                    n.ProfesorId,
                    Profesor = n.Profesor!.Nombre
                })
            })
            .FirstOrDefaultAsync();

        return estudiante is null ? NotFound() : Ok(estudiante);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateEstudianteDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            return BadRequest("El nombre del estudiante es obligatorio.");
        }

        var cursoExiste = await context.Cursos.AnyAsync(c => c.Id == dto.CursoId);
        if (!cursoExiste)
        {
            return BadRequest("El curso indicado no existe.");
        }

        var estudiante = new Estudiante
        {
            Nombre = dto.Nombre.Trim(),
            CursoId = dto.CursoId
        };

        context.Estudiantes.Add(estudiante);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = estudiante.Id }, estudiante);
    }

    [HttpPost("{id:int}/asignaturas/{asignaturaId:int}")]
    public async Task<IActionResult> Matricular(int id, int asignaturaId)
    {
        var estudiante = await context.Estudiantes.FindAsync(id);
        if (estudiante is null)
        {
            return NotFound("El estudiante no existe.");
        }

        var asignatura = await context.Asignaturas.FirstOrDefaultAsync(a => a.Id == asignaturaId);
        if (asignatura is null)
        {
            return NotFound("La asignatura no existe.");
        }

        if (asignatura.CursoId != estudiante.CursoId)
        {
            return BadRequest("El estudiante solo puede matricularse en asignaturas de su curso.");
        }

        var yaMatriculado = await context.EstudianteAsignaturas
            .AnyAsync(x => x.EstudianteId == id && x.AsignaturaId == asignaturaId);
        if (yaMatriculado)
        {
            return BadRequest("El estudiante ya esta matriculado en esta asignatura.");
        }

        context.EstudianteAsignaturas.Add(new EstudianteAsignatura
        {
            EstudianteId = id,
            AsignaturaId = asignaturaId
        });

        await context.SaveChangesAsync();
        return Ok();
    }
}
