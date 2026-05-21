using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Persistence.Context;
using Back.Api.Application.Dtos;
using Back.Api.Application.Abstractions.Security;
using Back.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Back.Api.Persistence.Repositories;

public class AdminDomainRepository(AppDbContext context, ICurrentSchoolContext currentSchoolContext) : IAdminDomainRepository
{

    public async Task<IEnumerable<AdminListItemDto>> GetAllAdminsAsync(CancellationToken cancellationToken = default)
        => await context.Admins
            .AsNoTracking()
            .Select(a => new AdminListItemDto { Id = a.Id, Nombre = a.Nombre, Correo = a.Cuenta!.Correo })
            .ToListAsync(cancellationToken);

    public Task<bool> CorreoDuplicadoAsync(string correo, CancellationToken cancellationToken = default)
        => context.Cuentas.AnyAsync(c => c.Correo == correo && c.ColegioId == currentSchoolContext.SchoolId, cancellationToken);

    public async Task<AdminListItemDto> CreateAdminAsync(string nombre, string correo, string contrasenaHash, CancellationToken cancellationToken = default)
    {
        var admin = new Admin
        {
            Nombre = nombre,
            Cuenta = new Cuenta
            {
                Correo = correo,
                Contrasena = contrasenaHash,
                Rol = "admin",
                ColegioId = currentSchoolContext.SchoolId
            }
        };
        context.Admins.Add(admin);
        await context.SaveChangesAsync(cancellationToken);
        return new AdminListItemDto { Id = admin.Id, Nombre = admin.Nombre, Correo = admin.Cuenta!.Correo };
    }

    public async Task<IEnumerable<AdminMatriculaListReadModelDto>> GetMatriculasAsync(CancellationToken cancellationToken = default)
        => await context.Estudiantes
            .AsNoTracking()
            .Include(e => e.EstudianteAsignaturas)
                .ThenInclude(ea => ea.Asignatura)
            .Include(e => e.Curso)
            .OrderBy(e => e.Nombre)
            .Select(e => new AdminMatriculaListReadModelDto
            {
                EstudianteId = e.Id,
                Estudiante = e.Nombre,
                CursoId = e.CursoId,
                Curso = e.Curso != null ? e.Curso.Nombre : null,
                Asignaturas = e.EstudianteAsignaturas
                    .Where(ea => !ea.IsDeleted)
                    .Select(ea => new AdminMatriculaAsignaturaReadModelDto
                    {
                        AsignaturaId = ea.AsignaturaId,
                        Asignatura = ea.Asignatura != null ? ea.Asignatura.Nombre : string.Empty
                    })
                    .OrderBy(a => a.Asignatura)
                    .ToList()
            })
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<AdminImparticionListReadModelDto>> GetImparticionesAsync(CancellationToken cancellationToken = default)
        => await context.ProfesorAsignaturaCursos
            .AsNoTracking()
            .OrderBy(imparticion => imparticion.Curso!.Nombre)
            .ThenBy(imparticion => imparticion.Asignatura!.Nombre)
            .Select(imparticion => new AdminImparticionListReadModelDto
            {
                ProfesorId = imparticion.ProfesorId,
                Profesor = imparticion.Profesor != null ? imparticion.Profesor.Nombre : string.Empty,
                AsignaturaId = imparticion.AsignaturaId,
                Asignatura = imparticion.Asignatura != null ? imparticion.Asignatura.Nombre : string.Empty,
                CursoId = imparticion.CursoId,
                Curso = imparticion.Curso != null ? imparticion.Curso.Nombre : string.Empty
            })
            .ToListAsync(cancellationToken);
}
