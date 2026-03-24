using Back.Api.Application.Dtos;

namespace Back.Api.Domain.Repositories;

public interface IEstudiantesDomainRepository
{
    // ── Checks ────────────────────────────────────────────────────────────
    Task<bool> ExisteAsync(int id);
    Task<bool> CorreoDuplicadoAsync(string correo);
    Task<bool> CorreoDuplicadoExceptAsync(string correo, int exceptId);
    Task<bool> CursoExisteAsync(int cursoId);
    Task<bool> AsignaturaExisteAsync(int asignaturaId);
    Task<bool> YaMatriculadoAsync(int estudianteId, int asignaturaId);
    Task<bool> AsignaturaEsDelCursoAsync(int asignaturaId, int cursoId);
    // ── Lookups ───────────────────────────────────────────────────────────
    Task<string?> GetCursoNombreAsync(int cursoId);
    // ── Queries ───────────────────────────────────────────────────────────
    Task<IEnumerable<EstudianteListItemDto>> GetAllAsync();
    Task<EstudianteDetalleDto?> GetDetalleAsync(int id);
    Task<AlumnoPanelDto?> GetPanelAlumnoAsync(int id);
    Task<AlumnoPanelResumenDto?> GetPanelResumenAsync(int id);
    Task<AlumnoMateriaDetalleDto?> GetMateriaDetalleAsync(int estudianteId, int asignaturaId);
    // ── Mutations ─────────────────────────────────────────────────────────
    Task<EstudianteListItemDto> CreateAsync(string nombre, string correo, int cursoId, string hash);
    Task MatricularAsync(int estudianteId, int asignaturaId);
    Task<EstudianteListItemDto?> UpdateAsync(int id, string nombre, string correo, int cursoId, string? hash);
    Task DeleteAsync(int id);
}
