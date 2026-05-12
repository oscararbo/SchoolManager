using Back.Api.Application.Common;
using Back.Api.Application.Dtos;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Back.Api.Application.Services;

public interface IEstudiantesService
{
    Task<ApplicationResult> GetAllEstudiantesAsync(CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetSimpleEstudiantesAsync(CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetEstudianteByIdAsync(int estudianteId, CancellationToken cancellationToken = default);
    Task<ApplicationResult> CreateEstudianteAsync(CreateEstudianteRequestDto createEstudianteRequestDto, CancellationToken cancellationToken = default);
    Task<ApplicationResult> UpdateEstudianteAsync(int estudianteId, UpdateEstudianteRequestDto updateEstudianteRequestDto, CancellationToken cancellationToken = default);
    Task<ApplicationResult> DeleteEstudianteAsync(int estudianteId, CancellationToken cancellationToken = default);
    Task<ApplicationResult> MatricularAsync(int estudianteId, int asignaturaId, CancellationToken cancellationToken = default);
    Task<ApplicationResult> DesmatricularAsync(int estudianteId, int asignaturaId, CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetPanelAlumnoAsync(int estudianteId, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetPanelResumenAsync(int estudianteId, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetMateriaDetalleAsync(int estudianteId, int asignaturaId, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApplicationResult> SubirSubmisionAsync(int estudianteId, int tareaId, IFormFile archivo, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetSubmisionesAsync(int estudianteId, int tareaId, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApplicationResult> DeleteSubmisionAsync(int estudianteId, int submisionId, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApplicationResult> MarcarTareaHechaAsync(int estudianteId, int tareaId, ClaimsPrincipal user, CancellationToken cancellationToken = default);
}
