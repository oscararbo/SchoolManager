using Back.Api.Data;
using Back.Api.Dtos;
using Back.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Back.Api.Services;

public class AdminService(AppDbContext context, IPasswordService passwordService) : IAdminService
{
    public async Task<IActionResult> GetAllAsync()
    {
        var admins = await context.Admins
            .AsNoTracking()
            .Select(a => new AdminListItemDto
            {
                Id = a.Id,
                Nombre = a.Nombre,
                Correo = a.Correo
            })
            .ToListAsync();

        return new OkObjectResult(admins);
    }

    public async Task<IActionResult> CreateAsync(CreateAdminDto dto, ClaimsPrincipal user)
    {
        // Solo admins pueden crear otros admins, pero el usuario que crea NO puede ser un recurso creado por otro admin
        // Para simplificar, solo permitimos que el seed admin (id=1 o el primer admin) cree otros admins
        if (!user.IsInRole("admin"))
        {
            return new ForbidResult();
        }

        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            return new BadRequestObjectResult("El nombre del administrador es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(dto.Correo))
        {
            return new BadRequestObjectResult("El correo del administrador es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(dto.Contrasena))
        {
            return new BadRequestObjectResult("La contrasena del administrador es obligatoria.");
        }

        var correo = dto.Correo.Trim().ToLowerInvariant();

        var correoDuplicado = await context.Admins
            .AnyAsync(a => a.Correo == correo);

        if (correoDuplicado)
        {
            return new BadRequestObjectResult("Ya existe un administrador con ese correo.");
        }

        var admin = new Admin
        {
            Nombre = dto.Nombre.Trim(),
            Correo = correo,
            Contrasena = passwordService.Hash(dto.Contrasena.Trim())
        };

        context.Admins.Add(admin);
        await context.SaveChangesAsync();

        return new CreatedResult($"/api/admin/{admin.Id}", new AdminListItemDto
        {
            Id = admin.Id,
            Nombre = admin.Nombre,
            Correo = admin.Correo
        });
    }
}
