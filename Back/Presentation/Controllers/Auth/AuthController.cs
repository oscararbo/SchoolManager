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
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class AuthController(IAuthService authService, IOptions<JwtOptions> jwtOptions) : ControllerBase
{
    private const string RefreshTokenCookie = "refresh_token";

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequestDto request)
    {
        var result = await authService.LoginAsync(request, HttpContext.RequestAborted);
        if (result.Type == ApplicationResultType.Ok && result.Value is LoginResponseDto dto)
        {
            SetRefreshTokenCookie(dto.RefreshToken);
            return Ok(new LoginClientResponseDto
            {
                Rol = dto.Rol, Id = dto.Id, Nombre = dto.Nombre,
                Correo = dto.Correo, Token = dto.Token,
                CursoId = dto.CursoId, Curso = dto.Curso
            });
        }
        return this.ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookie];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new ProblemDetails { Status = 401, Title = "Refresh token no encontrado." });

        var result = await authService.RefreshAsync(refreshToken, HttpContext.RequestAborted);
        if (result.Type == ApplicationResultType.Ok && result.Value is RefreshResponseDto dto)
        {
            SetRefreshTokenCookie(dto.RefreshToken);
            return Ok(new { token = dto.Token });
        }
        ClearRefreshTokenCookie();
        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
            Secure = false, // Set to true when deploying with HTTPS
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(jwtOptions.Value.RefreshExpiresDays),
            Path = "/api/auth"
        });
    }

    private void ClearRefreshTokenCookie()
    {
        Response.Cookies.Delete(RefreshTokenCookie, new CookieOptions { Path = "/api/auth" });
    }
}
