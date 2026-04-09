using Back.Api.Application.Dtos;
using Back.Api.Application.Services;
using Back.Api.Presentation.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back.Api.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDto request)
    {
        return this.ToActionResult(await authService.LoginAsync(request, HttpContext.RequestAborted));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequestDto request)
    {
        return this.ToActionResult(await authService.RefreshAsync(request, HttpContext.RequestAborted));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequestDto request)
    {
        return this.ToActionResult(await authService.LogoutAsync(request, User, HttpContext.RequestAborted));
    }
}
