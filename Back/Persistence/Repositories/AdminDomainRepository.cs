using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Persistence.Context;
using Back.Api.Application.Dtos;
using Back.Api.Application.Abstractions.Security;
using Back.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Back.Api.Persistence.Repositories;

public class AdminDomainRepository(AppDbContext context, ICurrentSchoolContext currentSchoolContext) : IAdminDomainRepository
{

    public async Task<IEnumerable<AdminHorarioAsignaturaReadModelDto>> GetHorariosAsync(CancellationToken cancellationToken = default)
        => await context.HorariosAsignaturas
            .AsNoTracking()
            .OrderBy(horario => horario.Asignatura!.Curso!.Nombre)
            .ThenBy(horario => horario.Asignatura!.Nombre)
            .ThenBy(horario => horario.DiaSemana)
            .ThenBy(horario => horario.HoraInicio)
            .Select(horario => new AdminHorarioAsignaturaReadModelDto
            {
                HorarioId = horario.Id,
                AsignaturaId = horario.AsignaturaId,
                Asignatura = horario.Asignatura != null ? horario.Asignatura.Nombre : string.Empty,
                CursoId = horario.Asignatura != null ? horario.Asignatura.CursoId : 0,
                Curso = horario.Asignatura != null && horario.Asignatura.Curso != null ? horario.Asignatura.Curso.Nombre : string.Empty,
                DiaSemana = horario.DiaSemana,
                HoraInicio = horario.HoraInicio.ToString("HH:mm"),
                HoraFin = horario.HoraFin.ToString("HH:mm"),
                Aula = horario.Aula
            })
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<AdminListItemDto>> GetAllAdminsAsync(CancellationToken cancellationToken = default)
        => await context.Admins
            .AsNoTracking()
            .Select(a => new AdminListItemDto { Id = a.Id, Nombre = a.Nombre, Correo = a.Cuenta!.Correo })
            .ToListAsync(cancellationToken);

    public Task<bool> AsignaturaExisteAsync(int asignaturaId, CancellationToken cancellationToken = default)
        => context.Asignaturas.AnyAsync(asignatura => asignatura.Id == asignaturaId, cancellationToken);

    public Task<bool> HorarioExisteAsync(int horarioId, CancellationToken cancellationToken = default)
        => context.HorariosAsignaturas.AnyAsync(horario => horario.Id == horarioId, cancellationToken);

    public Task<bool> HorarioDuplicadoAsync(int asignaturaId, int diaSemana, TimeOnly horaInicio, int? exceptHorarioId = null, CancellationToken cancellationToken = default)
        => context.HorariosAsignaturas.AnyAsync(horario =>
            horario.AsignaturaId == asignaturaId
            && horario.DiaSemana == diaSemana
            && horario.HoraInicio == horaInicio
            && (!exceptHorarioId.HasValue || horario.Id != exceptHorarioId.Value), cancellationToken);

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

    public async Task<AdminHorarioAsignaturaReadModelDto> CreateHorarioAsync(int asignaturaId, int diaSemana, TimeOnly horaInicio, TimeOnly horaFin, string? aula, CancellationToken cancellationToken = default)
    {
        var horario = new HorarioAsignatura
        {
            AsignaturaId = asignaturaId,
            DiaSemana = diaSemana,
            HoraInicio = horaInicio,
            HoraFin = horaFin,
            Aula = string.IsNullOrWhiteSpace(aula) ? null : aula.Trim()
        };

        context.HorariosAsignaturas.Add(horario);
        await context.SaveChangesAsync(cancellationToken);

        var created = await context.HorariosAsignaturas
            .AsNoTracking()
            .Where(item => item.Id == horario.Id)
            .Select(item => new AdminHorarioAsignaturaReadModelDto
            {
                HorarioId = item.Id,
                AsignaturaId = item.AsignaturaId,
                Asignatura = item.Asignatura != null ? item.Asignatura.Nombre : string.Empty,
                CursoId = item.Asignatura != null ? item.Asignatura.CursoId : 0,
                Curso = item.Asignatura != null && item.Asignatura.Curso != null ? item.Asignatura.Curso.Nombre : string.Empty,
                DiaSemana = item.DiaSemana,
                HoraInicio = item.HoraInicio.ToString("HH:mm"),
                HoraFin = item.HoraFin.ToString("HH:mm"),
                Aula = item.Aula
            })
            .FirstAsync(cancellationToken);

        return created;
    }

    public async Task<AdminHorarioAsignaturaReadModelDto?> UpdateHorarioAsync(int horarioId, int asignaturaId, int diaSemana, TimeOnly horaInicio, TimeOnly horaFin, string? aula, CancellationToken cancellationToken = default)
    {
        var horario = await context.HorariosAsignaturas
            .FirstOrDefaultAsync(item => item.Id == horarioId, cancellationToken);

        if (horario is null)
            return null;

        horario.AsignaturaId = asignaturaId;
        horario.DiaSemana = diaSemana;
        horario.HoraInicio = horaInicio;
        horario.HoraFin = horaFin;
        horario.Aula = string.IsNullOrWhiteSpace(aula) ? null : aula.Trim();

        await context.SaveChangesAsync(cancellationToken);

        return await context.HorariosAsignaturas
            .AsNoTracking()
            .Where(item => item.Id == horarioId)
            .Select(item => new AdminHorarioAsignaturaReadModelDto
            {
                HorarioId = item.Id,
                AsignaturaId = item.AsignaturaId,
                Asignatura = item.Asignatura != null ? item.Asignatura.Nombre : string.Empty,
                CursoId = item.Asignatura != null ? item.Asignatura.CursoId : 0,
                Curso = item.Asignatura != null && item.Asignatura.Curso != null ? item.Asignatura.Curso.Nombre : string.Empty,
                DiaSemana = item.DiaSemana,
                HoraInicio = item.HoraInicio.ToString("HH:mm"),
                HoraFin = item.HoraFin.ToString("HH:mm"),
                Aula = item.Aula
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task DeleteHorarioAsync(int horarioId, CancellationToken cancellationToken = default)
    {
        var horario = await context.HorariosAsignaturas
            .FirstOrDefaultAsync(item => item.Id == horarioId, cancellationToken);

        if (horario is null)
            return;

        horario.IsDeleted = true;
        await context.SaveChangesAsync(cancellationToken);
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
