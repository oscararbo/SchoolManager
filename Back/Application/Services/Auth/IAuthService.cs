using Back.Api.Application.Common;
using Back.Api.Application.Dtos;
using System.Security.Claims;

namespace Back.Api.Application.Services;

public interface IAuthService
{
    Task<ApplicationResult> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<ApplicationResult> RefreshAsync(RefreshRequestDto request, CancellationToken cancellationToken = default);
    Task<ApplicationResult> LogoutAsync(LogoutRequestDto request, ClaimsPrincipal user, CancellationToken cancellationToken = default);
}
