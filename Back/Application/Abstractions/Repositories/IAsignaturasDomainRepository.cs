using Back.Api.Application.Dtos;

namespace Back.Api.Application.Abstractions.Repositories;

public interface IAsignaturasDomainRepository
{
    Task<bool> ExisteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> CursoExisteAsync(int cursoId, CancellationToken cancellationToken = default);
    Task<bool> ExisteEnCursoAsync(int cursoId, string nombre, CancellationToken cancellationToken = default);
    Task<string?> GetCursoNombreAsync(int cursoId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AsignaturaResumenDto>> GetAllResumenAsync(CancellationToken cancellationToken = default);
    Task<AsignaturaDetalleDto?> GetDetalleAsync(int id, CancellationToken cancellationToken = default);
    Task<AsignaturaResumenDto> CreateAsync(string nombre, int cursoId, CancellationToken cancellationToken = default);
    Task<AsignaturaResumenDto?> UpdateAsync(int id, string nombre, int cursoId, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
