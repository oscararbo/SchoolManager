using Back.Api.Application.Common;
using Back.Api.Application.Dtos;
using System.Security.Claims;

namespace Back.Api.Application.Services;

public interface IAdminService
{
    Task<ApplicationResult> GetAllAdminsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ApplicationResult> CreateAdminAsync(CreateAdminRequestDto createAdminRequestDto, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetMatriculasAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetImparticionesAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetHorariosAsync(CancellationToken cancellationToken = default);
    Task<ApplicationResult> CreateHorarioAsync(CreateHorarioAsignaturaRequestDto requestDto, CancellationToken cancellationToken = default);
    Task<ApplicationResult> UpdateHorarioAsync(int horarioId, UpdateHorarioAsignaturaRequestDto requestDto, CancellationToken cancellationToken = default);
    Task<ApplicationResult> DeleteHorarioAsync(int horarioId, CancellationToken cancellationToken = default);
}
