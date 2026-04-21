using Back.Api.Application.Common;
using Back.Api.Application.Configuration;
using Back.Api.Application.Dtos;
using Back.Api.Application.Services;
using Back.Api.Presentation.Http;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Back.Api.Presentation.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
public class AuthController(IAuthService authService, IOptions<JwtOptions> jwtOptions, IWebHostEnvironment env) : ControllerBase
{
    private const string RefreshTokenCookie = "refresh_token";

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDto loginRequestDto)
    {
        var loginResult = await authService.LoginAsync(loginRequestDto, HttpContext.RequestAborted);
        if (loginResult.Type == ApplicationResultType.Ok && loginResult.Value is LoginResponseDto loginResponseDto)
        {
            SetRefreshTokenCookie(loginResponseDto.RefreshToken);
            return Ok(new LoginClientResponseDto
            {
                Rol = loginResponseDto.Rol, Id = loginResponseDto.Id, Nombre = loginResponseDto.Nombre,
                Correo = loginResponseDto.Correo, Token = loginResponseDto.Token,
                CursoId = loginResponseDto.CursoId, Curso = loginResponseDto.Curso
            });
        }
        return this.ToActionResult(loginResult);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookie];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new ProblemDetails { Status = 401, Title = "Refresh token no encontrado." });

        var refreshResult = await authService.RefreshAsync(refreshToken, HttpContext.RequestAborted);
        if (refreshResult.Type == ApplicationResultType.Ok && refreshResult.Value is RefreshResponseDto refreshResponseDto)
        {
            SetRefreshTokenCookie(refreshResponseDto.RefreshToken);
            return Ok(new { token = refreshResponseDto.Token });
        }
        ClearRefreshTokenCookie();
        return this.ToActionResult(refreshResult);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookie];
        ClearRefreshTokenCookie();
        if (!string.IsNullOrEmpty(refreshToken))
            await authService.LogoutAsync(refreshToken, User, HttpContext.RequestAborted);
        return NoContent();
    }

    private void SetRefreshTokenCookie(string token)
    {
        Response.Cookies.Append(RefreshTokenCookie, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !env.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(jwtOptions.Value.RefreshExpiresDays),
            Path = "/api/auth"
        });
    }

    private void ClearRefreshTokenCookie()
    {
        Response.Cookies.Delete(RefreshTokenCookie, new CookieOptions { Path = "/api/auth" });
    }
}

