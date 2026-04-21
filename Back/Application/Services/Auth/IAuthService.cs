using Back.Api.Application.Common;
using Back.Api.Application.Dtos;
using System.Security.Claims;

namespace Back.Api.Application.Services;

public interface IAuthService
{
    Task<ApplicationResult> LoginAsync(LoginRequestDto loginRequestDto, CancellationToken cancellationToken = default);
    Task<ApplicationResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<ApplicationResult> LogoutAsync(string refreshToken, ClaimsPrincipal user, CancellationToken cancellationToken = default);
}
