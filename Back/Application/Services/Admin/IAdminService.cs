using Back.Api.Application.Common;
using Back.Api.Application.Dtos;
using System.Security.Claims;

namespace Back.Api.Application.Services;

public interface IAdminService
{
    Task<ApplicationResult> GetAllAdminsAsync(CancellationToken cancellationToken = default);
    Task<ApplicationResult> CreateAdminAsync(CreateAdminRequestDto createAdminRequestDto, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetStatsAsync(CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetCursosStatsSelectorAsync(CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetStatsByCursoAsync(int cursoId, CancellationToken cancellationToken = default);
    Task<ApplicationResult> CompareCursosAsync(IEnumerable<int> cursoIds, CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetMatriculasAsync(CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetImparticionesAsync(CancellationToken cancellationToken = default);
}
