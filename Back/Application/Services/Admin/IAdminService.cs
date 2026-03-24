using Back.Api.Application.Common;
using Back.Api.Application.Dtos;
using System.Security.Claims;

namespace Back.Api.Application.Services;

public interface IAdminService
{
    Task<ApplicationResult> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ApplicationResult> CreateAsync(CreateAdminDto dto, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetStatsAsync(CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetNotasStatsAsync(CancellationToken cancellationToken = default);
}
