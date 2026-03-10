using Back.Api.Data;
using Back.Api.Dtos;
using Back.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Back.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AsignaturasController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var asignaturasBase = await context.Asignaturas
            .AsNoTracking()
            .Select(a => new
            {
                a.Id,
                a.Nombre,
                CursoId = a.CursoId,
                CursoNombre = a.Curso!.Nombre
            })
            .ToListAsync();

        var profesoresPorAsignatura = await context.ProfesorAsignaturaCursos
            .AsNoTracking()
            .Select(i => new
            {
                i.AsignaturaId,
                i.ProfesorId,
                ProfesorNombre = i.Profesor!.Nombre
            })
            .Distinct()
            .ToListAsync();

        var alumnosPorAsignatura = await context.EstudianteAsignaturas
            .AsNoTracking()
            .Select(ea => new
            {
                ea.AsignaturaId,
                EstudianteId = ea.EstudianteId,
                EstudianteNombre = ea.Estudiante!.Nombre
            })
            .Distinct()
            .ToListAsync();

        var asignaturas = asignaturasBase.Select(a => new
        {
            a.Id,
            a.Nombre,
            Curso = new
            {
                Id = a.CursoId,
                Nombre = a.CursoNombre
            },
            Profesores = profesoresPorAsignatura
                .Where(p => p.AsignaturaId == a.Id)
                .OrderBy(p => p.ProfesorNombre)
                .Select(p => new
                {
                    p.ProfesorId,
                    Nombre = p.ProfesorNombre
                })
                .ToList(),
            Alumnos = alumnosPorAsignatura
                .Where(e => e.AsignaturaId == a.Id)
                .OrderBy(e => e.EstudianteNombre)
                .Select(e => new
                {
                    Id = e.EstudianteId,
                    Nombre = e.EstudianteNombre
                })
                .ToList()
        });

        return Ok(asignaturas);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var asignatura = await context.Asignaturas
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new
            {
                a.Id,
                a.Nombre,
                CursoId = a.CursoId,
                Curso = a.Curso!.Nombre
            })
            .FirstOrDefaultAsync();

        if (asignatura is null)
        {
            return NotFound();
        }

        var imparticiones = await context.ProfesorAsignaturaCursos
            .AsNoTracking()
            .Where(i => i.AsignaturaId == id)
            .Select(i => new
            {
                i.ProfesorId,
                Profesor = i.Profesor!.Nombre
            })
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
                Notas = ea.Estudiante.Notas
                    .Where(n => n.AsignaturaId == id)
                    .Select(n => new
                    {
                        n.Id,
                        n.Valor
                    })
                    .ToList()
            })
            .OrderBy(x => x.Alumno)
            .ToListAsync();

        return Ok(new
        {
            asignatura.Id,
            asignatura.Nombre,
            Curso = new
            {
                Id = asignatura.CursoId,
                Nombre = asignatura.Curso
            },
            Imparticiones = imparticiones,
            Alumnos = alumnos
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAsignaturaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            return BadRequest("El nombre de la asignatura es obligatorio.");
        }

        var cursoExiste = await context.Cursos.AnyAsync(c => c.Id == dto.CursoId);
        if (!cursoExiste)
        {
            return BadRequest("El curso indicado no existe.");
        }

        var nombre = dto.Nombre.Trim();
        var duplicada = await context.Asignaturas.AnyAsync(a => a.CursoId == dto.CursoId && a.Nombre == nombre);
        if (duplicada)
        {
            return BadRequest("Ya existe esa asignatura en ese curso.");
        }

        var asignatura = new Asignatura
        {
            Nombre = nombre,
            CursoId = dto.CursoId
        };
        context.Asignaturas.Add(asignatura);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = asignatura.Id }, asignatura);
    }
}
