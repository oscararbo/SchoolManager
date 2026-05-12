using Back.Api.Application.Dtos;

namespace Back.Api.Application.Abstractions.Repositories;

public interface IEstudiantesDomainRepository
{
    Task<bool> ExisteAsync(int estudianteId, CancellationToken cancellationToken = default);
    Task<bool> CorreoDuplicadoAsync(string correo, CancellationToken cancellationToken = default);
    Task<bool> CorreoDuplicadoExceptAsync(string correo, int exceptEstudianteId, CancellationToken cancellationToken = default);
    Task<bool> DocumentoDuplicadoAsync(string documentoIdentidad, CancellationToken cancellationToken = default);
    Task<bool> DocumentoDuplicadoExceptAsync(string documentoIdentidad, int exceptEstudianteId, CancellationToken cancellationToken = default);
    Task<bool> CursoExisteAsync(int cursoId, CancellationToken cancellationToken = default);
    Task<bool> AsignaturaExisteAsync(int asignaturaId, CancellationToken cancellationToken = default);
    Task<bool> YaMatriculadoAsync(int estudianteId, int asignaturaId, CancellationToken cancellationToken = default);
    Task<bool> AsignaturaEsDelCursoAsync(int asignaturaId, int cursoId, CancellationToken cancellationToken = default);
    Task<string?> GetCursoNombreAsync(int cursoId, CancellationToken cancellationToken = default);
    Task<IEnumerable<EstudianteLookupDto>> GetSimpleEstudiantesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<EstudianteListItemDto>> GetAllEstudiantesAsync(CancellationToken cancellationToken = default);
    Task<EstudianteDetalleDto?> GetDetalleAsync(int estudianteId, CancellationToken cancellationToken = default);
    Task<AlumnoPanelDto?> GetPanelAlumnoAsync(int estudianteId, CancellationToken cancellationToken = default);
    Task<AlumnoPanelResumenDto?> GetPanelResumenAsync(int estudianteId, CancellationToken cancellationToken = default);
    Task<AlumnoMateriaDetalleDto?> GetMateriaDetalleAsync(int estudianteId, int asignaturaId, CancellationToken cancellationToken = default);
    Task<EstudianteListItemDto> CreateEstudianteAsync(string nombre, string correo, int cursoId, string contrasenaHash, string apellidos, string dni, string telefono, DateOnly fechaNacimiento, CancellationToken cancellationToken = default);
    Task MatricularAsync(int estudianteId, int asignaturaId, CancellationToken cancellationToken = default);
    Task DesmatricularAsync(int estudianteId, int asignaturaId, CancellationToken cancellationToken = default);
    Task<EstudianteListItemDto?> UpdateEstudianteAsync(int estudianteId, string nombre, int cursoId, string apellidos, string dni, string telefono, DateOnly fechaNacimiento, CancellationToken cancellationToken = default);
    Task DeleteEstudianteAsync(int estudianteId, CancellationToken cancellationToken = default);
    Task<bool> EstudianteMatriculadoEnTareaAsync(int estudianteId, int tareaId, CancellationToken cancellationToken = default);
    Task<TareaSubmisionDto> UpsertSubmisionEstudianteAsync(int estudianteId, int tareaId, string nombreArchivo, string rutaArchivo, long tamanoBytes, CancellationToken cancellationToken = default);
    Task<List<TareaSubmisionDto>> GetSubmisionesEstudianteAsync(int estudianteId, int tareaId, CancellationToken cancellationToken = default);
    Task<bool> DeleteSubmisionEstudianteAsync(int estudianteId, int submisionId, CancellationToken cancellationToken = default);
    Task<TareaSubmisionDto> MarcarTareaHechaAsync(int estudianteId, int tareaId, CancellationToken cancellationToken = default);
}
