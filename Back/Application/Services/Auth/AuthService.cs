using Back.Api.Application.Common;
using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Abstractions.Security;
using Back.Api.Application.Configuration;
using Back.Api.Application.Dtos;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Back.Api.Application.Services;

public class AuthService(IAuthDomainRepository authDomain, IOptions<JwtOptions> jwtOptions, IPasswordService passwordService) : IAuthService
{
    public async Task<ApplicationResult> LoginAsync(LoginRequestDto loginRequestDto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(loginRequestDto.Correo) || string.IsNullOrWhiteSpace(loginRequestDto.Contrasena))
            return ApplicationResult.BadRequest("Correo y contrasena son obligatorios.");

        var correo = loginRequestDto.Correo.Trim().ToLowerInvariant();
        var contrasena = loginRequestDto.Contrasena.Trim();

        var admin = await authDomain.FindAdminByCorreoAsync(correo, cancellationToken);
        if (admin?.Cuenta is not null && passwordService.Verify(admin.Cuenta.Contrasena, contrasena))
        {
            var token = GenerarToken(admin.Id, admin.Cuenta.Correo, Roles.Admin);
            var refreshToken = await authDomain.CreateRefreshTokenAsync(admin.Id, Roles.Admin, jwtOptions.Value.RefreshExpiresDays, cancellationToken);
            return ApplicationResult.Ok(new LoginResponseDto { Rol = Roles.Admin, Id = admin.Id, Nombre = admin.Nombre, Correo = admin.Cuenta.Correo, Token = token, RefreshToken = refreshToken });
        }

        var profesor = await authDomain.FindProfesorByCorreoAsync(correo, cancellationToken);
        if (profesor?.Cuenta is not null && passwordService.Verify(profesor.Cuenta.Contrasena, contrasena))
        {
            var token = GenerarToken(profesor.Id, profesor.Cuenta.Correo, Roles.Profesor);
            var refreshToken = await authDomain.CreateRefreshTokenAsync(profesor.Id, Roles.Profesor, jwtOptions.Value.RefreshExpiresDays, cancellationToken);
            return ApplicationResult.Ok(new LoginResponseDto { Rol = Roles.Profesor, Id = profesor.Id, Nombre = profesor.Nombre, Correo = profesor.Cuenta.Correo, Token = token, RefreshToken = refreshToken });
        }

        var estudiante = await authDomain.FindEstudianteByCorreoAsync(correo, cancellationToken);
        if (estudiante?.Cuenta is not null && passwordService.Verify(estudiante.Cuenta.Contrasena, contrasena))
        {
            var token = GenerarToken(estudiante.Id, estudiante.Cuenta.Correo, Roles.Alumno);
            var refreshToken = await authDomain.CreateRefreshTokenAsync(estudiante.Id, Roles.Alumno, jwtOptions.Value.RefreshExpiresDays, cancellationToken);
            return ApplicationResult.Ok(new LoginResponseDto { Rol = Roles.Alumno, Id = estudiante.Id, Nombre = estudiante.Nombre, Correo = estudiante.Cuenta.Correo, Token = token, RefreshToken = refreshToken, CursoId = estudiante.CursoId, Curso = estudiante.Curso?.Nombre });
        }

        return ApplicationResult.Unauthorized("Credenciales incorrectas.");
    }

    public async Task<ApplicationResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return ApplicationResult.Unauthorized("Refresh token no valido.");

        var storedToken = await authDomain.FindRefreshTokenAsync(refreshToken, cancellationToken);
        if (storedToken is null || !storedToken.IsActive)
            return ApplicationResult.Unauthorized("Refresh token no valido o expirado.");

        var correo = await authDomain.ObtenerCorreoAsync(storedToken.UserId, storedToken.Rol, cancellationToken);
        if (correo is null)
        {
            await authDomain.RevokeTokenAsync(storedToken, cancellationToken);
            return ApplicationResult.Unauthorized("Usuario no valido.");
        }

        await authDomain.RevokeTokenAsync(storedToken, cancellationToken);
        var newAccessToken = GenerarToken(storedToken.UserId, correo, storedToken.Rol);
        var newRefreshToken = await authDomain.CreateRefreshTokenAsync(storedToken.UserId, storedToken.Rol, jwtOptions.Value.RefreshExpiresDays, cancellationToken);

        return ApplicationResult.Ok(new RefreshResponseDto { Token = newAccessToken, RefreshToken = newRefreshToken });
    }

    public async Task<ApplicationResult> LogoutAsync(string refreshToken, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return ApplicationResult.BadRequest("Refresh token obligatorio.");

        var userIdClaim = user.FindFirstValue("id") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        var rol = user.FindFirstValue(ClaimTypes.Role);

        if (!int.TryParse(userIdClaim, out var userId) || string.IsNullOrWhiteSpace(rol))
            return ApplicationResult.Unauthorized();

        var storedToken = await authDomain.FindRefreshTokenAsync(refreshToken, cancellationToken);
        if (storedToken is null || !storedToken.IsActive)
            return ApplicationResult.Unauthorized("Refresh token no valido o expirado.");

        if (storedToken.UserId != userId || !string.Equals(storedToken.Rol, rol, StringComparison.OrdinalIgnoreCase))
            return ApplicationResult.Forbidden();

        await authDomain.RevokeTokenAsync(storedToken, cancellationToken);
        return ApplicationResult.NoContent();
    }

    private string GenerarToken(int userId, string correo, string rol)
    {
        var options = jwtOptions.Value;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, correo),
            new(ClaimTypes.Role, rol),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("id", userId.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(options.ExpiresMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
