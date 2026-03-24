using Back.Api.Application.Common;
using Back.Api.Application.Dtos;
using System.Security.Claims;

namespace Back.Api.Application.Services;

public interface IEstudiantesService
{
    Task<ApplicationResult> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApplicationResult> CreateAsync(CreateEstudianteDto dto, CancellationToken cancellationToken = default);
    Task<ApplicationResult> UpdateAsync(int id, UpdateEstudianteDto dto, CancellationToken cancellationToken = default);
    Task<ApplicationResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<ApplicationResult> MatricularAsync(int id, int asignaturaId, CancellationToken cancellationToken = default);
    Task<ApplicationResult> DesmatricularAsync(int id, int asignaturaId, CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetPanelAlumnoAsync(int id, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetPanelResumenAsync(int id, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetMateriaDetalleAsync(int id, int asignaturaId, ClaimsPrincipal user, CancellationToken cancellationToken = default);
}
