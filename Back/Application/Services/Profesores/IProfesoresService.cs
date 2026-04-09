using Back.Api.Application.Common;
using Back.Api.Application.Dtos;
using System.Security.Claims;

namespace Back.Api.Application.Services;

public interface IProfesoresService
{
    Task<ApplicationResult> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetSimpleAsync(CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApplicationResult> CreateAsync(CreateProfesorRequestDto dto, CancellationToken cancellationToken = default);
    Task<ApplicationResult> UpdateAsync(int id, UpdateProfesorRequestDto dto, CancellationToken cancellationToken = default);
    Task<ApplicationResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetPanelProfesorAsync(int id, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetAlumnosDeAsignaturaAsync(int profesorId, int asignaturaId, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetAlumnosResumenDeAsignaturaAsync(int profesorId, int asignaturaId, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetAlumnoDetalleDeAsignaturaAsync(int profesorId, int asignaturaId, int estudianteId, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetCalificacionesDeTareaAsync(int profesorId, int asignaturaId, int tareaId, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApplicationResult> AsignarImparticionAsync(int profesorId, AsignarImparticionRequestDto dto, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApplicationResult> EliminarImparticionAsync(int profesorId, int asignaturaId, int cursoId, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApplicationResult> PonerNotaAsync(int profesorId, PonerNotaRequestDto dto, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApplicationResult> CrearTareaAsync(int profesorId, CreateTareaRequestDto dto, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetTareasDeAsignaturaAsync(int profesorId, int asignaturaId, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetTareasConNotasAsync(int asignaturaId, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetStatsAsync(int profesorId, ClaimsPrincipal user, CancellationToken cancellationToken = default);
}
