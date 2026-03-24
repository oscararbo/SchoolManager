using Back.Api.Application.Dtos;

namespace Back.Api.Domain.Repositories;

public interface IAsignaturasDomainRepository
{
    Task<bool> ExisteAsync(int id);
    Task<bool> CursoExisteAsync(int cursoId);
    Task<bool> ExisteEnCursoAsync(int cursoId, string nombre);
    Task<string?> GetCursoNombreAsync(int cursoId);
    Task<IEnumerable<AsignaturaResumenDto>> GetAllResumenAsync();
    Task<AsignaturaDetalleDto?> GetDetalleAsync(int id);
    Task<AsignaturaResumenDto> CreateAsync(string nombre, int cursoId);
    Task<AsignaturaResumenDto?> UpdateAsync(int id, string nombre, int cursoId);
    Task DeleteAsync(int id);
}
