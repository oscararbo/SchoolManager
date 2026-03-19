using Back.Api.Data;
using Back.Api.Dtos;
using Back.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Back.Api.Services;

public class CursosService(AppDbContext context) : ICursosService
{
    public async Task<IActionResult> GetAllAsync()
    {
        var cursosBase = await context.Cursos
            .AsNoTracking()
            .Select(c => new CursoSimpleDto { Id = c.Id, Nombre = c.Nombre })
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
                .Select(a => new CursoAsignaturaDto
                {
                    Id = a.Id,
                    Nombre = a.Nombre,
                    ProfesorId = a.ProfesorId,
                    Profesor = a.Profesor
                })
                .ToList()
        }).Select(x => new CursoResumenDto
        {
            Id = x.Id,
            Nombre = x.Nombre,
            Asignaturas = x.Asignaturas
        });

        return new OkObjectResult(cursos);
    }

    public async Task<IActionResult> GetByIdAsync(int id)
    {
        var curso = await context.Cursos
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CursoSimpleDto { Id = c.Id, Nombre = c.Nombre })
            .FirstOrDefaultAsync();

        if (curso is null)
        {
            return new NotFoundObjectResult("El curso no existe.");
        }

        var alumnos = await context.Estudiantes
            .AsNoTracking()
            .Where(e => e.CursoId == id)
            .Select(e => new CursoAlumnoDto { Id = e.Id, Nombre = e.Nombre })
            .OrderBy(e => e.Nombre)
            .ToListAsync();

        var asignaturas = await context.Asignaturas
            .AsNoTracking()
            .Where(a => a.CursoId == id)
            .Select(a => new CursoDetalleAsignaturaDto
            {
                Id = a.Id,
                Nombre = a.Nombre,
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

        return new OkObjectResult(new CursoDetalleDto
        {
            Id = curso.Id,
            Nombre = curso.Nombre,
            Alumnos = alumnos,
            Asignaturas = asignaturas
        });
    }

    public async Task<IActionResult> CreateAsync(CreateCursoDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            return new BadRequestObjectResult("El nombre del curso es obligatorio.");
        }

        var curso = new Curso { Nombre = dto.Nombre.Trim() };
        context.Cursos.Add(curso);
        await context.SaveChangesAsync();
        return new CreatedResult($"/api/cursos/{curso.Id}", new CursoSimpleDto { Id = curso.Id, Nombre = curso.Nombre });
    }

    public async Task<IActionResult> UpdateAsync(int id, UpdateCursoDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return new BadRequestObjectResult("El nombre del curso es obligatorio.");

        var curso = await context.Cursos.FindAsync(id);
        if (curso is null)
            return new NotFoundObjectResult("El curso no existe.");

        curso.Nombre = dto.Nombre.Trim();
        await context.SaveChangesAsync();
        return new OkObjectResult(new CursoSimpleDto { Id = curso.Id, Nombre = curso.Nombre });
    }

    public async Task<IActionResult> DeleteAsync(int id)
    {
        var curso = await context.Cursos.FindAsync(id);
        if (curso is null)
            return new NotFoundObjectResult("El curso no existe.");

        var tieneEstudiantes = await context.Estudiantes.AnyAsync(e => e.CursoId == id);
        if (tieneEstudiantes)
            return new BadRequestObjectResult("No se puede eliminar el curso porque tiene alumnos asignados.");

        var tieneAsignaturas = await context.Asignaturas.AnyAsync(a => a.CursoId == id);
        if (tieneAsignaturas)
            return new BadRequestObjectResult("No se puede eliminar el curso porque tiene asignaturas. Elimínalas primero.");

        context.Cursos.Remove(curso);
        await context.SaveChangesAsync();
        return new NoContentResult();
    }
}
