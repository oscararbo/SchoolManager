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
                    AsignaturaId = n.AsignaturaId,
                    Asignatura = n.Asignatura!.Nombre,
                    Valor = n.Valor,
                    ProfesorId = n.ProfesorId,
                    Profesor = n.Profesor!.Nombre
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

        var correo = dto.Correo.Trim().ToLowerInvariant();

        var correoDuplicado = await context.Estudiantes
            .AnyAsync(e => e.Correo.ToLower() == correo);

        if (correoDuplicado)
        {
            return new BadRequestObjectResult("Ya existe un estudiante con ese correo.");
        }

        var cursoExiste = await context.Cursos.AnyAsync(c => c.Id == dto.CursoId);
        if (!cursoExiste)
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
            Curso = null
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
            .Select(e => new
            {
                e.Id,
                e.Nombre,
                CursoId = e.CursoId,
                Curso = e.Curso!.Nombre
            })
            .FirstOrDefaultAsync();

        if (estudiante is null)
        {
            return new NotFoundObjectResult("El estudiante no existe.");
        }

        var filas = await context.EstudianteAsignaturas
            .AsNoTracking()
            .Where(ea => ea.EstudianteId == id)
            .Select(ea => new AlumnoMateriaDto
            {
                AsignaturaId = ea.AsignaturaId,
                Asignatura = ea.Asignatura!.Nombre,
                Profesor = ea.Asignatura.ProfesorAsignaturaCursos
                    .Where(pac => pac.CursoId == estudiante.CursoId)
                    .Select(pac => pac.Profesor!.Nombre)
                    .FirstOrDefault(),
                Nota = context.Notas
                    .Where(n => n.EstudianteId == id && n.AsignaturaId == ea.AsignaturaId)
                    .Select(n => (decimal?)n.Valor)
                    .FirstOrDefault()
            })
            .OrderBy(x => x.Asignatura)
            .ToListAsync();

        return new OkObjectResult(new AlumnoPanelDto
        {
            Id = estudiante.Id,
            Nombre = estudiante.Nombre,
            Curso = new AlumnoCursoDto
            {
                CursoId = estudiante.CursoId,
                Curso = estudiante.Curso
            },
            Materias = filas
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
}
