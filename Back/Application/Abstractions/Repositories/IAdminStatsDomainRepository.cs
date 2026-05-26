using Back.Api.Application.Dtos;

namespace Back.Api.Application.Abstractions.Repositories;

public interface IAdminStatsDomainRepository
{
    Task<AdminStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);
    Task<AdminTop5StatsDto> GetTop5Async(CancellationToken cancellationToken = default);
    Task<IEnumerable<CursoStatsSelectorDto>> GetCursosStatsSelectorAsync(CancellationToken cancellationToken = default);
    Task<CursoNotasStatsResponseDto?> GetStatsByCursoAsync(int cursoId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CursoComparacionItemDto>> CompareCursosAsync(IEnumerable<int> cursoIds, CancellationToken cancellationToken = default);
}
