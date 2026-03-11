using Back.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Back.Api.Controllers;

public record LoginRequest(string Correo, string Contrasena);

[ApiController]
[Route("api/[controller]")]
public class AuthController(AppDbContext context) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Correo) || string.IsNullOrWhiteSpace(request.Contrasena))
        {
            return BadRequest("Correo y contrasena son obligatorios.");
        }

        var correo = request.Correo.Trim().ToLowerInvariant();
        var contrasena = request.Contrasena.Trim();

        var profesor = await context.Profesores
            .AsNoTracking()
            .Where(p => p.Correo.ToLower() == correo && p.Contrasena == contrasena)
            .Select(p => new
            {
                p.Id,
                p.Nombre
            })
            .FirstOrDefaultAsync();

        if (profesor is not null)
        {
            return Ok(new
            {
                Rol = "profesor",
                profesor.Id,
                profesor.Nombre,
                Correo = correo
            });
        }

        var estudiante = await context.Estudiantes
            .AsNoTracking()
            .Where(e => e.Correo.ToLower() == correo && e.Contrasena == contrasena)
            .Select(e => new
            {
                e.Id,
                e.Nombre,
                e.CursoId,
                Curso = e.Curso!.Nombre
            })
            .FirstOrDefaultAsync();

        if (estudiante is not null)
        {
            return Ok(new
            {
                Rol = "alumno",
                estudiante.Id,
                estudiante.Nombre,
                Correo = correo,
                estudiante.CursoId,
                estudiante.Curso
            });
        }

        return Unauthorized("Credenciales incorrectas.");
    }
}
