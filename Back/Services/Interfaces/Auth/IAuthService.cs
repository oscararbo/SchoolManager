using Back.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Back.Api.Services;

public interface IAuthService
{
    Task<IActionResult> LoginAsync(LoginRequest request);
    Task<IActionResult> RefreshAsync(RefreshRequest request);
    Task<IActionResult> LogoutAsync(LogoutRequest request, ClaimsPrincipal user);
}
