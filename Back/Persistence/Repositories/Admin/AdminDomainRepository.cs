using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Persistence.Context;
using Back.Api.Application.Dtos;
using Back.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Back.Api.Persistence.Repositories;

public class AdminDomainRepository(AppDbContext context) : IAdminDomainRepository
{
    public async Task<IEnumerable<AdminListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => await context.Admins
            .AsNoTracking()
            .Select(a => new AdminListItemDto { Id = a.Id, Nombre = a.Nombre, Correo = a.Correo })
            .ToListAsync(cancellationToken);

    public Task<bool> CorreoDuplicadoAsync(string correo, CancellationToken cancellationToken = default)
        => context.Admins.AnyAsync(a => a.Correo == correo);

    public async Task<AdminListItemDto> CreateAsync(string nombre, string correo, string hash, CancellationToken cancellationToken = default)
    {
        var admin = new Admin { Nombre = nombre, Correo = correo, Contrasena = hash };
        context.Admins.Add(admin);
        await context.SaveChangesAsync();
        return new AdminListItemDto { Id = admin.Id, Nombre = admin.Nombre, Correo = admin.Correo };
    }

    public async Task<IEnumerable<AdminMatriculaListItemDto>> GetMatriculasAsync(CancellationToken cancellationToken = default)
        => await context.Estudiantes
            .AsNoTracking()
            .OrderBy(e => e.Nombre)
            .Select(e => new AdminMatriculaListItemDto
            {
                EstudianteId = e.Id,
                Estudiante = e.Nombre,
                CursoId = e.CursoId,
                Curso = e.Curso != null ? e.Curso.Nombre : null,
                Asignaturas = context.EstudianteAsignaturas
                    .Where(ea => ea.EstudianteId == e.Id)
                    .Join(context.Asignaturas,
                        ea => ea.AsignaturaId,
                        a => a.Id,
                        (_, a) => new AdminMatriculaAsignaturaItemDto
                        {
                            AsignaturaId = a.Id,
                            Asignatura = a.Nombre
                        })
                    .OrderBy(a => a.Asignatura)
                    .ToList()
            })
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<AdminImparticionListItemDto>> GetImparticionesAsync(CancellationToken cancellationToken = default)
        => await context.ProfesorAsignaturaCursos
            .AsNoTracking()
            .OrderBy(x => x.Curso!.Nombre)
            .ThenBy(x => x.Asignatura!.Nombre)
            .Select(x => new AdminImparticionListItemDto
            {
                ProfesorId = x.ProfesorId,
                Profesor = x.Profesor != null ? x.Profesor.Nombre : string.Empty,
                AsignaturaId = x.AsignaturaId,
                Asignatura = x.Asignatura != null ? x.Asignatura.Nombre : string.Empty,
                CursoId = x.CursoId,
                Curso = x.Curso != null ? x.Curso.Nombre : string.Empty
            })
            .ToListAsync(cancellationToken);
}
