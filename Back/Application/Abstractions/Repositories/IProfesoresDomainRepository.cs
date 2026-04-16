using Back.Api.Application.Dtos;

namespace Back.Api.Application.Abstractions.Repositories;

public record ProfesorAlumnoResumenRow(int EstudianteId, string Alumno);
public record ProfesorTareaCalificacionRow(int EstudianteId, string Alumno, decimal? Valor);

public interface IProfesoresDomainRepository
{
    Task<bool> ProfesorExisteAsync(int profesorId, CancellationToken cancellationToken = default);
    Task<bool> ProfesorImparteAsignaturaAsync(int profesorId, int asignaturaId, CancellationToken cancellationToken = default);
    Task<bool> ProfesorImparteTareaAsync(int profesorId, int tareaId, CancellationToken cancellationToken = default);
    Task<bool> CorreoDuplicadoAsync(string correo, CancellationToken cancellationToken = default);
    Task<bool> CorreoDuplicadoExceptAsync(string correo, int exceptId, CancellationToken cancellationToken = default);
    Task<bool> CursoExisteAsync(int cursoId, CancellationToken cancellationToken = default);
    Task<bool> AsignaturaYaTieneOtroProfesorAsync(int asignaturaId, int profesorId, CancellationToken cancellationToken = default);
    Task<bool> ImparticionExisteAsync(int profesorId, int asignaturaId, int cursoId, CancellationToken cancellationToken = default);
    Task<bool> EstudianteMatriculadoAsync(int estudianteId, int asignaturaId, CancellationToken cancellationToken = default);
    Task<bool> ProfesorImparteAlCursoAsync(int profesorId, int asignaturaId, int cursoId, CancellationToken cancellationToken = default);
    Task<bool> TareaDuplicadaAsync(int asignaturaId, int trimestre, string nombre, CancellationToken cancellationToken = default);
    Task<AsignaturaInfoDto?> GetAsignaturaInfoAsync(int asignaturaId, CancellationToken cancellationToken = default);
    Task<(int Id, int CursoId)?> GetAsignaturaBasicaAsync(int asignaturaId, CancellationToken cancellationToken = default);
    Task<(int Id, int AsignaturaId, int ProfesorId)?> GetTareaInfoAsync(int tareaId, CancellationToken cancellationToken = default);
    Task<int?> GetEstudianteCursoAsync(int estudianteId, CancellationToken cancellationToken = default);
    Task<TareaResumenDto?> GetTareaResumenAsync(int tareaId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProfesorLookupDto>> GetSimpleAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ProfesorListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProfesorDetalleDto?> GetDetalleAsync(int id, CancellationToken cancellationToken = default);
    Task<ProfesorPanelDto?> GetPanelAsync(int id, CancellationToken cancellationToken = default);
    Task<List<TareaResumenDto>> GetTareasDeAsignaturaAsync(int asignaturaId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TareaResumenDto>> GetTareasDeProfesorEnAsignaturaAsync(int profesorId, int asignaturaId, CancellationToken cancellationToken = default);
    Task<List<ProfesorAlumnoResumenRow>> GetAlumnosResumenAsync(int asignaturaId, CancellationToken cancellationToken = default);
    Task<AsignaturaAlumnosResumenResponseDto?> GetAlumnosResumenResponseAsync(int asignaturaId, CancellationToken cancellationToken = default);
    Task<AsignaturaCalificacionesTareaResponseDto?> GetCalificacionesTareaResponseAsync(int asignaturaId, int tareaId, CancellationToken cancellationToken = default);
    Task<List<ProfesorTareaCalificacionRow>> GetCalificacionesTareaAsync(int asignaturaId, int tareaId, CancellationToken cancellationToken = default);
    Task<ProfesorAlumnoDetalleDto?> GetAlumnoDetalleAsync(int asignaturaId, int estudianteId, CancellationToken cancellationToken = default);
    Task<AsignaturaAlumnosResponseDto?> GetAlumnosCompletoAsync(int asignaturaId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TareaConNotasDto>> GetTareasConNotasAsync(int asignaturaId, CancellationToken cancellationToken = default);
    Task<ProfesorStatsDto?> GetStatsAsync(int profesorId, CancellationToken cancellationToken = default);
    Task<ProfesorListItemDto> CreateAsync(string nombre, string correo, string hash, CancellationToken cancellationToken = default);
    Task<ProfesorListItemDto?> UpdateAsync(int id, string nombre, string correo, string? hash, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task AsignarImparticionAsync(int profesorId, int asignaturaId, int cursoId, CancellationToken cancellationToken = default);
    Task EliminarImparticionAsync(int profesorId, int asignaturaId, int cursoId, CancellationToken cancellationToken = default);
    Task SetNotaAsync(int estudianteId, int tareaId, decimal valor, CancellationToken cancellationToken = default);
    Task<TareaDetalleDto> CrearTareaAsync(string nombre, int trimestre, int asignaturaId, int profesorId, CancellationToken cancellationToken = default);
}
