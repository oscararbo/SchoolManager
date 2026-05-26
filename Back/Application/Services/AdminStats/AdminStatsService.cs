using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Common;
using Back.Api.Application.Dtos;

namespace Back.Api.Application.Services;

public class AdminStatsService(IAdminStatsDomainRepository adminStatsDomain) : IAdminStatsService
{
    public async Task<ApplicationResult> GetStatsAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await adminStatsDomain.GetStatsAsync(cancellationToken));

    public async Task<ApplicationResult> GetTop5Async(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await adminStatsDomain.GetTop5Async(cancellationToken));

    public async Task<ApplicationResult> GetCursosStatsSelectorAsync(CancellationToken cancellationToken = default)
        => ApplicationResult.Ok(await adminStatsDomain.GetCursosStatsSelectorAsync(cancellationToken));

    public async Task<ApplicationResult> GetStatsByCursoAsync(int cursoId, CancellationToken cancellationToken = default)
    {
        var courseStats = await adminStatsDomain.GetStatsByCursoAsync(cursoId, cancellationToken);
        if (courseStats is null)
            return ApplicationResult.NotFound("El curso no existe.");

        return ApplicationResult.Ok(courseStats);
    }

    public async Task<ApplicationResult> CompareCursosAsync(IEnumerable<int> cursoIds, CancellationToken cancellationToken = default)
    {
        var ids = cursoIds
            .Where(id => id > 0)
            .Distinct()
            .Take(6)
            .ToList();

        if (ids.Count < 2)
            return ApplicationResult.BadRequest("Selecciona al menos 2 cursos para comparar.");

        return ApplicationResult.Ok(new ComparacionCursosResponseDto
        {
            Cursos = (await adminStatsDomain.CompareCursosAsync(ids, cancellationToken)).ToList()
        });
    }
}
