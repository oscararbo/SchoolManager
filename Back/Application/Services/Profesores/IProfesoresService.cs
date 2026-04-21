using Back.Api.Application.Common;
using Back.Api.Application.Dtos;
using System.Security.Claims;

namespace Back.Api.Application.Services;

public interface IProfesoresService
{
    Task<ApplicationResult> GetAllProfesoresAsync(CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetSimpleProfesoresAsync(CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetProfesorByIdAsync(int profesorId, CancellationToken cancellationToken = default);
    Task<ApplicationResult> CreateProfesorAsync(CreateProfesorRequestDto createProfesorRequestDto, CancellationToken cancellationToken = default);
    Task<ApplicationResult> UpdateProfesorAsync(int profesorId, UpdateProfesorRequestDto updateProfesorRequestDto, CancellationToken cancellationToken = default);
    Task<ApplicationResult> DeleteProfesorAsync(int profesorId, CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetPanelProfesorAsync(int profesorId, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetAlumnosDeAsignaturaAsync(int profesorId, int asignaturaId, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetAlumnosResumenDeAsignaturaAsync(int profesorId, int asignaturaId, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetAlumnoDetalleDeAsignaturaAsync(int profesorId, int asignaturaId, int estudianteId, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetCalificacionesDeTareaAsync(int profesorId, int asignaturaId, int tareaId, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApplicationResult> AsignarImparticionAsync(int profesorId, AsignarImparticionRequestDto asignarImparticionRequestDto, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApplicationResult> EliminarImparticionAsync(int profesorId, int asignaturaId, int cursoId, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApplicationResult> PonerNotaAsync(int profesorId, PonerNotaRequestDto ponerNotaRequestDto, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApplicationResult> CrearTareaAsync(int profesorId, CreateTareaRequestDto createTareaRequestDto, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetTareasDeAsignaturaAsync(int profesorId, int asignaturaId, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetTareasConNotasAsync(int asignaturaId, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetStatsAsync(int profesorId, ClaimsPrincipal user, CancellationToken cancellationToken = default);
}
