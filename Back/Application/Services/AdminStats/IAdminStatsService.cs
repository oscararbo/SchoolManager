using Back.Api.Application.Common;
using Back.Api.Application.Dtos;

namespace Back.Api.Application.Services;

public interface IAdminStatsService
{
    Task<ApplicationResult> GetStatsAsync(CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetTop5Async(CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetCursosStatsSelectorAsync(CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetStatsByCursoAsync(int cursoId, CancellationToken cancellationToken = default);
    Task<ApplicationResult> CompareCursosAsync(IEnumerable<int> cursoIds, CancellationToken cancellationToken = default);
}
