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

    public async Task<IAdminDomainRepository.PagedResult<AdminListItemDto>> GetAllAdminsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = context.Admins.AsNoTracking();

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(a => a.Nombre)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(a => new AdminListItemDto
            {
                Id = a.Id,
                Nombre = a.Nombre,
                Correo = a.Cuenta!.Correo
            })
            .ToListAsync(cancellationToken);

        return new IAdminDomainRepository.PagedResult<AdminListItemDto>(items, total);
    }

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

    public async Task<bool> HorarioSolapaEnCursoAsync(int asignaturaId, int diaSemana, TimeOnly horaInicio, TimeOnly horaFin, int? exceptHorarioId = null, CancellationToken cancellationToken = default)
    {
        var cursoId = await context.Asignaturas
            .AsNoTracking()
            .Where(asignatura => asignatura.Id == asignaturaId)
            .Select(asignatura => (int?)asignatura.CursoId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!cursoId.HasValue)
            return false;

        return await context.HorariosAsignaturas.AnyAsync(horario =>
            horario.Asignatura != null
            && horario.Asignatura.CursoId == cursoId.Value
            && horario.DiaSemana == diaSemana
            && (!exceptHorarioId.HasValue || horario.Id != exceptHorarioId.Value)
            && horario.HoraInicio < horaFin
            && horaInicio < horario.HoraFin,
            cancellationToken);
    }

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

    public async Task<IAdminDomainRepository.PagedResult<AdminMatriculaListReadModelDto>> GetMatriculasAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = context.Estudiantes
            .AsNoTracking()
            .Include(e => e.Curso)
            .Include(e => e.EstudianteAsignaturas)
                .ThenInclude(ea => ea.Asignatura);

        var total = await query.CountAsync(cancellationToken);

        var estudiantes = await query
            .OrderBy(e => e.Nombre)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = estudiantes.Select(e => new AdminMatriculaListReadModelDto
        {
            EstudianteId = e.Id,
            Estudiante = e.Nombre,
            CursoId = e.CursoId,
            Curso = e.Curso?.Nombre,
            Asignaturas = e.EstudianteAsignaturas
                .Where(ea => !ea.IsDeleted)
                .OrderBy(a => a.Asignatura!.Nombre)
                .Select(ea => new AdminMatriculaAsignaturaReadModelDto
                {
                    AsignaturaId = ea.AsignaturaId,
                    Asignatura = ea.Asignatura?.Nombre ?? ""
                })
                .ToList()
        });

        return new IAdminDomainRepository.PagedResult<AdminMatriculaListReadModelDto>(items, total);
    }

    public async Task<IAdminDomainRepository.PagedResult<AdminImparticionListReadModelDto>> GetImparticionesAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = context.ProfesorAsignaturaCursos
            .AsNoTracking()
            .Include(i => i.Profesor)
            .Include(i => i.Asignatura)
            .Include(i => i.Curso);

        var total = await query.CountAsync(cancellationToken);

        var imparticiones = await query
            .OrderBy(i => i.Curso!.Nombre)
            .ThenBy(i => i.Asignatura!.Nombre)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = imparticiones.Select(i => new AdminImparticionListReadModelDto
        {
            ProfesorId = i.ProfesorId,
            Profesor = i.Profesor?.Nombre ?? "",
            AsignaturaId = i.AsignaturaId,
            Asignatura = i.Asignatura?.Nombre ?? "",
            CursoId = i.CursoId,
            Curso = i.Curso?.Nombre ?? ""
        });

        return new IAdminDomainRepository.PagedResult<AdminImparticionListReadModelDto>(items, total);
    }
}
