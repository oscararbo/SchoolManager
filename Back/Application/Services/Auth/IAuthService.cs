using Back.Api.Application.Common;
using Back.Api.Application.Dtos;
using System.Security.Claims;

namespace Back.Api.Application.Services;

public interface IAuthService
{
    Task<ApplicationResult> LoginAsync(LoginRequest request);
    Task<ApplicationResult> RefreshAsync(RefreshRequest request);
    Task<ApplicationResult> LogoutAsync(LogoutRequest request, ClaimsPrincipal user);
}
