using Back.Api.Application.Common;
using Back.Api.Application.Dtos;
using System.Security.Claims;

namespace Back.Api.Application.Services;

public interface IProfesoresService
{
    Task<ApplicationResult> GetAllAsync();
    Task<ApplicationResult> GetByIdAsync(int id);
    Task<ApplicationResult> CreateAsync(CreateProfesorDto dto);
    Task<ApplicationResult> UpdateAsync(int id, UpdateProfesorDto dto);
    Task<ApplicationResult> DeleteAsync(int id);
    Task<ApplicationResult> GetPanelProfesorAsync(int id, ClaimsPrincipal user);
    Task<ApplicationResult> GetAlumnosDeAsignaturaAsync(int profesorId, int asignaturaId, ClaimsPrincipal user);
    Task<ApplicationResult> GetAlumnosResumenDeAsignaturaAsync(int profesorId, int asignaturaId, ClaimsPrincipal user);
    Task<ApplicationResult> GetAlumnoDetalleDeAsignaturaAsync(int profesorId, int asignaturaId, int estudianteId, ClaimsPrincipal user);
    Task<ApplicationResult> GetCalificacionesDeTareaAsync(int profesorId, int asignaturaId, int tareaId, ClaimsPrincipal user);
    Task<ApplicationResult> AsignarImparticionAsync(int profesorId, AsignarImparticionDto dto, ClaimsPrincipal user);
    Task<ApplicationResult> PonerNotaAsync(int profesorId, PonerNotaDto dto, ClaimsPrincipal user);
    Task<ApplicationResult> CrearTareaAsync(int profesorId, CreateTareaDto dto, ClaimsPrincipal user);
    Task<ApplicationResult> GetTareasDeAsignaturaAsync(int profesorId, int asignaturaId, ClaimsPrincipal user);
    Task<ApplicationResult> GetTareasConNotasAsync(int asignaturaId, ClaimsPrincipal user);
}
