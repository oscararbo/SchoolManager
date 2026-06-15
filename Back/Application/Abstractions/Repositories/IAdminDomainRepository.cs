using Back.Api.Application.Dtos;

namespace Back.Api.Application.Abstractions.Repositories;

public interface IAdminDomainRepository
{
    public record PagedResult<T>(IEnumerable<T> Items, int Total);
    Task<PagedResult<AdminListItemDto>> GetAllAdminsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> CorreoDuplicadoAsync(string correo, CancellationToken cancellationToken = default);
    Task<AdminListItemDto> CreateAdminAsync(string nombre, string correo, string contrasenaHash, CancellationToken cancellationToken = default);
    Task<PagedResult<AdminMatriculaListReadModelDto>> GetMatriculasAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<AdminImparticionListReadModelDto>> GetImparticionesAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<AdminHorarioAsignaturaReadModelDto>> GetHorariosAsync(CancellationToken cancellationToken = default);
    Task<bool> AsignaturaExisteAsync(int asignaturaId, CancellationToken cancellationToken = default);
    Task<bool> HorarioExisteAsync(int horarioId, CancellationToken cancellationToken = default);
    Task<bool> HorarioDuplicadoAsync(int asignaturaId, int diaSemana, TimeOnly horaInicio, int? exceptHorarioId = null, CancellationToken cancellationToken = default);
    Task<bool> HorarioSolapaEnCursoAsync(int asignaturaId, int diaSemana, TimeOnly horaInicio, TimeOnly horaFin, int? exceptHorarioId = null, CancellationToken cancellationToken = default);
    Task<AdminHorarioAsignaturaReadModelDto> CreateHorarioAsync(int asignaturaId, int diaSemana, TimeOnly horaInicio, TimeOnly horaFin, string? aula, CancellationToken cancellationToken = default);
    Task<AdminHorarioAsignaturaReadModelDto?> UpdateHorarioAsync(int horarioId, int asignaturaId, int diaSemana, TimeOnly horaInicio, TimeOnly horaFin, string? aula, CancellationToken cancellationToken = default);
    Task DeleteHorarioAsync(int horarioId, CancellationToken cancellationToken = default);
}