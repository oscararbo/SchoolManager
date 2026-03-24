using Back.Api.Application.Dtos;

namespace Back.Api.Domain.Repositories;

public interface ICursosDomainRepository
{
    Task<bool> ExisteAsync(int id);
    Task<bool> TieneEstudiantesAsync(int id);
    Task<bool> TieneAsignaturasAsync(int id);
    Task<CursoSimpleDto?> GetSimpleAsync(int id);
    Task<IEnumerable<CursoResumenDto>> GetAllResumenAsync();
    Task<CursoDetalleDto?> GetDetalleAsync(int id);
    Task<CursoSimpleDto> CreateAsync(string nombre);
    Task<CursoSimpleDto?> UpdateAsync(int id, string nombre);
    Task DeleteAsync(int id);
}
