using Back.Api.Application.Common;
using Back.Api.Application.Dtos;
using System.Security.Claims;

namespace Back.Api.Application.Services;

public interface IEstudiantesService
{
    Task<ApplicationResult> GetAllAsync();
    Task<ApplicationResult> GetByIdAsync(int id);
    Task<ApplicationResult> CreateAsync(CreateEstudianteDto dto);
    Task<ApplicationResult> UpdateAsync(int id, UpdateEstudianteDto dto);
    Task<ApplicationResult> DeleteAsync(int id);
    Task<ApplicationResult> MatricularAsync(int id, int asignaturaId);
    Task<ApplicationResult> GetPanelAlumnoAsync(int id, ClaimsPrincipal user);
    Task<ApplicationResult> GetPanelResumenAsync(int id, ClaimsPrincipal user);
    Task<ApplicationResult> GetMateriaDetalleAsync(int id, int asignaturaId, ClaimsPrincipal user);
}
