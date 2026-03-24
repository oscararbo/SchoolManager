using Back.Api.Application.Dtos;

namespace Back.Api.Application.Abstractions.Repositories;

public interface ICursosDomainRepository
{
    Task<bool> ExisteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> TieneEstudiantesAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> TieneAsignaturasAsync(int id, CancellationToken cancellationToken = default);
    Task<CursoSimpleDto?> GetSimpleAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<CursoResumenDto>> GetAllResumenAsync(CancellationToken cancellationToken = default);
    Task<CursoDetalleDto?> GetDetalleAsync(int id, CancellationToken cancellationToken = default);
    Task<CursoSimpleDto> CreateAsync(string nombre, CancellationToken cancellationToken = default);
    Task<CursoSimpleDto?> UpdateAsync(int id, string nombre, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
