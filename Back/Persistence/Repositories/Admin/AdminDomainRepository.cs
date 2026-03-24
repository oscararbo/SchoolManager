using Back.Api.Persistence.Context;
using Back.Api.Application.Dtos;
using Back.Api.Domain.Entities;
using Back.Api.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Back.Api.Persistence.Repositories;

public class AdminDomainRepository(AppDbContext context) : IAdminDomainRepository
{
    public async Task<IEnumerable<AdminListItemDto>> GetAllAsync()
        => await context.Admins
            .AsNoTracking()
            .Select(a => new AdminListItemDto { Id = a.Id, Nombre = a.Nombre, Correo = a.Correo })
            .ToListAsync();

    public Task<bool> CorreoDuplicadoAsync(string correo)
        => context.Admins.AnyAsync(a => a.Correo == correo);

    public async Task<AdminListItemDto> CreateAsync(string nombre, string correo, string hash)
    {
        var admin = new Admin { Nombre = nombre, Correo = correo, Contrasena = hash };
        context.Admins.Add(admin);
        await context.SaveChangesAsync();
        return new AdminListItemDto { Id = admin.Id, Nombre = admin.Nombre, Correo = admin.Correo };
    }
}
