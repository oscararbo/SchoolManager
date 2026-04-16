using Back.Api.Application.Dtos;

namespace Back.Api.Application.Abstractions.Repositories;

public interface IEstudiantesDomainRepository
{
    Task<bool> ExisteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> CorreoDuplicadoAsync(string correo, CancellationToken cancellationToken = default);
    Task<bool> CorreoDuplicadoExceptAsync(string correo, int exceptId, CancellationToken cancellationToken = default);
    Task<bool> CursoExisteAsync(int cursoId, CancellationToken cancellationToken = default);
    Task<bool> AsignaturaExisteAsync(int asignaturaId, CancellationToken cancellationToken = default);
    Task<bool> YaMatriculadoAsync(int estudianteId, int asignaturaId, CancellationToken cancellationToken = default);
    Task<bool> AsignaturaEsDelCursoAsync(int asignaturaId, int cursoId, CancellationToken cancellationToken = default);
    Task<string?> GetCursoNombreAsync(int cursoId, CancellationToken cancellationToken = default);
    Task<IEnumerable<EstudianteLookupDto>> GetSimpleAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<EstudianteListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<EstudianteDetalleDto?> GetDetalleAsync(int id, CancellationToken cancellationToken = default);
    Task<AlumnoPanelDto?> GetPanelAlumnoAsync(int id, CancellationToken cancellationToken = default);
    Task<AlumnoPanelResumenDto?> GetPanelResumenAsync(int id, CancellationToken cancellationToken = default);
    Task<AlumnoMateriaDetalleDto?> GetMateriaDetalleAsync(int estudianteId, int asignaturaId, CancellationToken cancellationToken = default);
    Task<EstudianteListItemDto> CreateAsync(string nombre, string correo, int cursoId, string hash, string apellidos, string dni, string telefono, DateOnly fechaNacimiento, CancellationToken cancellationToken = default);
    Task MatricularAsync(int estudianteId, int asignaturaId, CancellationToken cancellationToken = default);
    Task DesmatricularAsync(int estudianteId, int asignaturaId, CancellationToken cancellationToken = default);
    Task<EstudianteListItemDto?> UpdateAsync(int id, string nombre, string correo, int cursoId, string? hash, string apellidos, string dni, string telefono, DateOnly fechaNacimiento, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
