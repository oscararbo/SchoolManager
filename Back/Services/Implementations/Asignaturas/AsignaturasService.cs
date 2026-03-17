using Back.Api.Data;
using Back.Api.Dtos;
using Back.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Back.Api.Services;

public class AsignaturasService(AppDbContext context) : IAsignaturasService
{
    public async Task<IActionResult> GetAllAsync()
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
            Curso = new AsignaturaCursoDto
            {
                Id = a.CursoId,
                Nombre = a.CursoNombre
            },
            Profesores = profesoresPorAsignatura
                .Where(p => p.AsignaturaId == a.Id)
                .OrderBy(p => p.ProfesorNombre)
                .Select(p => new AsignaturaProfesorDto
                {
                    ProfesorId = p.ProfesorId,
                    Nombre = p.ProfesorNombre
                })
                .ToList(),
            Alumnos = alumnosPorAsignatura
                .Where(e => e.AsignaturaId == a.Id)
                .OrderBy(e => e.EstudianteNombre)
                .Select(e => new AsignaturaAlumnoSimpleDto
                {
                    Id = e.EstudianteId,
                    Nombre = e.EstudianteNombre
                })
                .ToList()
        }).Select(x => new AsignaturaResumenDto
        {
            Id = x.Id,
            Nombre = x.Nombre,
            Curso = x.Curso,
            Profesores = x.Profesores,
            Alumnos = x.Alumnos
        });

        return new OkObjectResult(asignaturas);
    }

    public async Task<IActionResult> GetByIdAsync(int id)
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
            return new NotFoundResult();
        }

        var imparticiones = await context.ProfesorAsignaturaCursos
            .AsNoTracking()
            .Where(i => i.AsignaturaId == id)
            .Select(i => new AsignaturaImparticionDto
            {
                ProfesorId = i.ProfesorId,
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
                Notas = context.Notas
                    .Where(n => n.EstudianteId == ea.EstudianteId && n.Tarea!.AsignaturaId == id)
                    .Select(n => new AsignaturaNotaSimpleDto
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

        return new OkObjectResult(new AsignaturaDetalleDto
        {
            Id = asignatura.Id,
            Nombre = asignatura.Nombre,
            Curso = new AsignaturaCursoDto
            {
                Id = asignatura.CursoId,
                Nombre = asignatura.Curso
            },
            Imparticiones = imparticiones,
            Alumnos = alumnos.Select(a => new AsignaturaAlumnoDetalleDto
            {
                EstudianteId = a.EstudianteId,
                Alumno = a.Alumno,
                Notas = a.Notas
            }).ToList()
        });
    }

    public async Task<IActionResult> CreateAsync(CreateAsignaturaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            return new BadRequestObjectResult("El nombre de la asignatura es obligatorio.");
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

        var nombre = dto.Nombre.Trim();
        var duplicada = await context.Asignaturas.AnyAsync(a => a.CursoId == dto.CursoId && a.Nombre == nombre);
        if (duplicada)
        {
            return new BadRequestObjectResult("Ya existe esa asignatura en ese curso.");
        }

        var asignatura = new Asignatura
        {
            Nombre = nombre,
            CursoId = dto.CursoId
        };
        context.Asignaturas.Add(asignatura);
        await context.SaveChangesAsync();

        var response = new AsignaturaResumenDto
        {
            Id = asignatura.Id,
            Nombre = asignatura.Nombre,
            Curso = new AsignaturaCursoDto
            {
                Id = curso.Id,
                Nombre = curso.Nombre
            },
            Profesores = new List<AsignaturaProfesorDto>(),
            Alumnos = new List<AsignaturaAlumnoSimpleDto>()
        };

        return new CreatedResult($"/api/asignaturas/{asignatura.Id}", response);
    }
}
