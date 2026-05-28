using Back.Api.Application.Common;
using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Abstractions.Security;
using Back.Api.Application.Configuration;
using Back.Api.Application.Dtos;
using System.Security.Claims;

namespace Back.Api.Application.Services;

public class AdminService(
    IAdminDomainRepository adminDomain,
    IPasswordService passwordService) : IAdminService
{
    public async Task<ApplicationResult> GetAllAdminsAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await adminDomain.GetAllAdminsAsync(cancellationToken));

    public async Task<ApplicationResult> GetMatriculasAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await adminDomain.GetMatriculasAsync(cancellationToken));

    public async Task<ApplicationResult> GetImparticionesAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await adminDomain.GetImparticionesAsync(cancellationToken));

    public async Task<ApplicationResult> GetHorariosAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await adminDomain.GetHorariosAsync(cancellationToken));

    public async Task<ApplicationResult> CreateAdminAsync(CreateAdminRequestDto createAdminRequestDto, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!user.IsInRole(Roles.Admin))
            return ApplicationResult.Forbidden("No tienes permisos para crear administradores.");

        var email = createAdminRequestDto.Correo.Trim().ToLowerInvariant();
        if (await adminDomain.CorreoDuplicadoAsync(email, cancellationToken))
            return ApplicationResult.BadRequest("Ya existe un administrador con ese email.");

        var createdAdmin = await adminDomain.CreateAdminAsync(createAdminRequestDto.Nombre.Trim(), email, passwordService.Hash(createAdminRequestDto.Contrasena.Trim()), cancellationToken);
        return ApplicationResult.Created($"/api/admin/{createdAdmin.Id}", createdAdmin);
    }

    public async Task<ApplicationResult> CreateHorarioAsync(CreateHorarioAsignaturaRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateHorarioRequestAsync(requestDto.AsignaturaId, requestDto.DiaSemana, requestDto.HoraInicio, requestDto.HoraFin, null, cancellationToken);
        if (validation is not null)
            return validation;

        var horaInicio = TimeOnly.ParseExact(requestDto.HoraInicio.Trim(), "HH:mm");
        var horaFin = TimeOnly.ParseExact(requestDto.HoraFin.Trim(), "HH:mm");

        var created = await adminDomain.CreateHorarioAsync(requestDto.AsignaturaId, requestDto.DiaSemana, horaInicio, horaFin, requestDto.Aula, cancellationToken);
        return ApplicationResult.Created($"/api/admin/horarios/{created.HorarioId}", created);
    }

    public async Task<ApplicationResult> UpdateHorarioAsync(int horarioId, UpdateHorarioAsignaturaRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        if (!await adminDomain.HorarioExisteAsync(horarioId, cancellationToken))
            return ApplicationResult.NotFound("El horario no existe.");

        var validation = await ValidateHorarioRequestAsync(requestDto.AsignaturaId, requestDto.DiaSemana, requestDto.HoraInicio, requestDto.HoraFin, horarioId, cancellationToken);
        if (validation is not null)
            return validation;

        var horaInicio = TimeOnly.ParseExact(requestDto.HoraInicio.Trim(), "HH:mm");
        var horaFin = TimeOnly.ParseExact(requestDto.HoraFin.Trim(), "HH:mm");
        var updated = await adminDomain.UpdateHorarioAsync(horarioId, requestDto.AsignaturaId, requestDto.DiaSemana, horaInicio, horaFin, requestDto.Aula, cancellationToken);

        return updated is null
            ? ApplicationResult.NotFound("El horario no existe.")
            : ApplicationResult.Ok(updated);
    }

    public async Task<ApplicationResult> DeleteHorarioAsync(int horarioId, CancellationToken cancellationToken = default)
    {
        if (!await adminDomain.HorarioExisteAsync(horarioId, cancellationToken))
            return ApplicationResult.NotFound("El horario no existe.");

        await adminDomain.DeleteHorarioAsync(horarioId, cancellationToken);
        return ApplicationResult.NoContent();
    }

    private async Task<ApplicationResult?> ValidateHorarioRequestAsync(int asignaturaId, int diaSemana, string horaInicioRaw, string horaFinRaw, int? exceptHorarioId, CancellationToken cancellationToken)
    {
        if (asignaturaId <= 0)
            return ApplicationResult.BadRequest("La asignatura es obligatoria.");

        if (!await adminDomain.AsignaturaExisteAsync(asignaturaId, cancellationToken))
            return ApplicationResult.NotFound("La asignatura no existe.");

        if (diaSemana is < 1 or > 5)
            return ApplicationResult.BadRequest("El dia de la semana debe estar entre 1 (lunes) y 5 (viernes).");

        if (!TimeOnly.TryParseExact(horaInicioRaw?.Trim(), "HH:mm", out var horaInicio))
            return ApplicationResult.BadRequest("La hora de inicio debe tener formato HH:mm.");

        if (!TimeOnly.TryParseExact(horaFinRaw?.Trim(), "HH:mm", out var horaFin))
            return ApplicationResult.BadRequest("La hora de fin debe tener formato HH:mm.");

        if (horaInicio >= horaFin)
            return ApplicationResult.BadRequest("La hora de fin debe ser posterior a la hora de inicio.");

        if (await adminDomain.HorarioDuplicadoAsync(asignaturaId, diaSemana, horaInicio, exceptHorarioId, cancellationToken))
            return ApplicationResult.BadRequest("Ya existe un horario para esa asignatura, dia y hora de inicio.");

        if (await adminDomain.HorarioSolapaEnCursoAsync(asignaturaId, diaSemana, horaInicio, horaFin, exceptHorarioId, cancellationToken))
            return ApplicationResult.BadRequest("Ese curso ya tiene otra asignatura en ese tramo horario.");

        return null;
    }
}
