using Back.Api.Application.Dtos;

namespace Back.Api.Application.Abstractions.Repositories;

public interface ICursosDomainRepository
{
    Task<bool> ExisteAsync(int cursoId, CancellationToken cancellationToken = default);
    Task<bool> TieneEstudiantesAsync(int cursoId, CancellationToken cancellationToken = default);
    Task<bool> TieneAsignaturasAsync(int cursoId, CancellationToken cancellationToken = default);
    Task<CursoLookupDto?> GetCursoLookupAsync(int cursoId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CursoResumenDto>> GetAllResumenAsync(CancellationToken cancellationToken = default);
    Task<CursoDetalleDto?> GetDetalleAsync(int cursoId, CancellationToken cancellationToken = default);
    Task<CursoLookupDto> CreateCursoAsync(string nombre, CancellationToken cancellationToken = default);
    Task<CursoLookupDto?> UpdateCursoAsync(int cursoId, string nombre, CancellationToken cancellationToken = default);
    Task DeleteCursoAsync(int cursoId, CancellationToken cancellationToken = default);
}
