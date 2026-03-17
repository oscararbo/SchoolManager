using Back.Api.Data;
using Back.Api.Dtos;
using Back.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Back.Api.Services;

public class AuthService(AppDbContext context, IConfiguration configuration, IPasswordService passwordService) : IAuthService
{
    public async Task<IActionResult> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Correo) || string.IsNullOrWhiteSpace(request.Contrasena))
        {
            return new BadRequestObjectResult("Correo y contrasena son obligatorios.");
        }

        var correo = request.Correo.Trim().ToLowerInvariant();
        var contrasena = request.Contrasena.Trim();

        var profesor = await context.Profesores
            .FirstOrDefaultAsync(p => p.Correo.ToLower() == correo);

        if (profesor is not null && passwordService.Verify(profesor.Contrasena, contrasena))
        {
            var rol = profesor.EsAdmin ? "admin" : "profesor";
            var token = GenerarToken(profesor.Id, correo, rol);
            var refreshToken = await CrearRefreshTokenAsync(profesor.Id, rol);

            return new OkObjectResult(new LoginResponseDto
            {
                Rol = rol,
                Id = profesor.Id,
                Nombre = profesor.Nombre,
                Correo = correo,
                Token = token,
                RefreshToken = refreshToken
            });
        }

        var estudiante = await context.Estudiantes
            .Include(e => e.Curso)
            .FirstOrDefaultAsync(e => e.Correo.ToLower() == correo);

        if (estudiante is not null && passwordService.Verify(estudiante.Contrasena, contrasena))
        {
            var token = GenerarToken(estudiante.Id, correo, "alumno");
            var refreshToken = await CrearRefreshTokenAsync(estudiante.Id, "alumno");

            return new OkObjectResult(new LoginResponseDto
            {
                Rol = "alumno",
                Id = estudiante.Id,
                Nombre = estudiante.Nombre,
                Correo = correo,
                CursoId = estudiante.CursoId,
                Curso = estudiante.Curso?.Nombre,
                Token = token,
                RefreshToken = refreshToken
            });
        }

        return new UnauthorizedObjectResult("Credenciales incorrectas.");
    }

    public async Task<IActionResult> RefreshAsync(RefreshRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return new UnauthorizedObjectResult("Refresh token no valido.");
        }

        var storedToken = await context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

        if (storedToken is null || !storedToken.IsActive)
        {
            return new UnauthorizedObjectResult("Refresh token no valido o expirado.");
        }

        var correo = await ObtenerCorreoUsuarioAsync(storedToken.UserId, storedToken.Rol);
        if (correo is null)
        {
            storedToken.RevokedAtUtc = DateTime.UtcNow;
            await context.SaveChangesAsync();
            return new UnauthorizedObjectResult("Usuario no valido.");
        }

        storedToken.RevokedAtUtc = DateTime.UtcNow;
        var newAccessToken = GenerarToken(storedToken.UserId, correo, storedToken.Rol);
        var newRefreshToken = await CrearRefreshTokenAsync(storedToken.UserId, storedToken.Rol);

        return new OkObjectResult(new RefreshResponseDto
        {
            Token = newAccessToken,
            RefreshToken = newRefreshToken
        });
    }

    public async Task<IActionResult> LogoutAsync(LogoutRequest request, ClaimsPrincipal user)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return new BadRequestObjectResult("Refresh token obligatorio.");
        }

        var userIdClaim = user.FindFirstValue("id") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        var rol = user.FindFirstValue(ClaimTypes.Role);

        if (!int.TryParse(userIdClaim, out var userId) || string.IsNullOrWhiteSpace(rol))
        {
            return new UnauthorizedResult();
        }

        var storedToken = await context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

        if (storedToken is null || !storedToken.IsActive)
        {
            return new UnauthorizedObjectResult("Refresh token no valido o expirado.");
        }

        if (storedToken.UserId != userId || !string.Equals(storedToken.Rol, rol, StringComparison.OrdinalIgnoreCase))
        {
            return new ForbidResult();
        }

        storedToken.RevokedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return new NoContentResult();
    }

    private string GenerarToken(int userId, string correo, string rol)
    {
        var jwtKey = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key no esta configurado.");
        var jwtIssuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer no esta configurado.");
        var jwtAudience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience no esta configurado.");
        var expiresMinutes = int.TryParse(configuration["Jwt:ExpiresMinutes"], out var minutes) ? minutes : 120;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, correo),
            new(ClaimTypes.Role, rol),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("id", userId.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> CrearRefreshTokenAsync(int userId, string rol)
    {
        var refreshDays = int.TryParse(configuration["Jwt:RefreshExpiresDays"], out var days) ? days : 7;
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());

        context.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            Rol = rol,
            Token = token,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(refreshDays)
        });

        await context.SaveChangesAsync();
        return token;
    }

    private async Task<string?> ObtenerCorreoUsuarioAsync(int userId, string rol)
    {
        if (rol == "profesor" || rol == "admin")
        {
            return await context.Profesores
                .Where(p => p.Id == userId)
                .Select(p => p.Correo)
                .FirstOrDefaultAsync();
        }

        if (rol == "alumno")
        {
            return await context.Estudiantes
                .Where(e => e.Id == userId)
                .Select(e => e.Correo)
                .FirstOrDefaultAsync();
        }

        return null;
    }
}
