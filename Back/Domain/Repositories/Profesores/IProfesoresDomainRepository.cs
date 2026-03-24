using Back.Api.Application.Dtos;

namespace Back.Api.Domain.Repositories;

public record ProfesorAlumnoResumenRow(int EstudianteId, string Alumno);
public record ProfesorTareaCalificacionRow(int EstudianteId, string Alumno, decimal? Valor);

public interface IProfesoresDomainRepository
{
    // ── Existence / validation checks ─────────────────────────────────────
    Task<bool> ProfesorExisteAsync(int profesorId);
    Task<bool> ProfesorImparteAsignaturaAsync(int profesorId, int asignaturaId);
    Task<bool> ProfesorImparteTareaAsync(int profesorId, int tareaId);
    Task<bool> CorreoDuplicadoAsync(string correo);
    Task<bool> CorreoDuplicadoExceptAsync(string correo, int exceptId);
    Task<bool> CursoExisteAsync(int cursoId);
    Task<bool> AsignaturaYaTieneOtroProfesorAsync(int asignaturaId, int profesorId);
    Task<bool> ImparticionExisteAsync(int profesorId, int asignaturaId, int cursoId);
    Task<bool> EstudianteMatriculadoAsync(int estudianteId, int asignaturaId);
    Task<bool> ProfesorImparteAlCursoAsync(int profesorId, int asignaturaId, int cursoId);
    Task<bool> TareaDuplicadaAsync(int asignaturaId, int trimestre, string nombre);
    // ── Simple lookups ────────────────────────────────────────────────────
    Task<AsignaturaInfoDto?> GetAsignaturaInfoAsync(int asignaturaId);
    Task<(int Id, int CursoId)?> GetAsignaturaBasicaAsync(int asignaturaId);
    Task<(int Id, int AsignaturaId, int ProfesorId)?> GetTareaInfoAsync(int tareaId);
    Task<int?> GetEstudianteCursoAsync(int estudianteId);
    Task<TareaResumenDto?> GetTareaResumenAsync(int tareaId);
    // ── Queries ───────────────────────────────────────────────────────────
    Task<IEnumerable<ProfesorListItemDto>> GetAllAsync();
    Task<ProfesorDetalleDto?> GetDetalleAsync(int id);
    Task<ProfesorPanelDto?> GetPanelAsync(int id);
    Task<List<TareaResumenDto>> GetTareasDeAsignaturaAsync(int asignaturaId);
    Task<IEnumerable<TareaResumenDto>> GetTareasDeProfesorEnAsignaturaAsync(int profesorId, int asignaturaId);
    Task<List<ProfesorAlumnoResumenRow>> GetAlumnosResumenAsync(int asignaturaId);
    Task<List<ProfesorTareaCalificacionRow>> GetCalificacionesTareaAsync(int asignaturaId, int tareaId);
    Task<ProfesorAlumnoDetalleDto?> GetAlumnoDetalleAsync(int asignaturaId, int estudianteId);
    Task<AsignaturaAlumnosResponseDto?> GetAlumnosCompletoAsync(int asignaturaId);
    Task<IEnumerable<TareaConNotasDto>> GetTareasConNotasAsync(int asignaturaId);
    // ── Mutations ─────────────────────────────────────────────────────────
    Task<ProfesorListItemDto> CreateAsync(string nombre, string correo, string hash);
    Task<ProfesorListItemDto?> UpdateAsync(int id, string nombre, string correo, string? hash);
    Task DeleteAsync(int id);
    Task AsignarImparticionAsync(int profesorId, int asignaturaId, int cursoId);
    Task SetNotaAsync(int estudianteId, int tareaId, decimal valor);
    Task<TareaDetalleDto> CrearTareaAsync(string nombre, int trimestre, int asignaturaId, int profesorId);
}
