using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Persistence.Context;
using Back.Api.Application.Dtos;
using Back.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Back.Api.Persistence.Repositories;

public class AdminDomainRepository(AppDbContext context) : IAdminDomainRepository
{
    public async Task<IEnumerable<AdminListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => await context.Admins
            .AsNoTracking()
            .Select(a => new AdminListItemDto { Id = a.Id, Nombre = a.Nombre, Correo = a.Correo })
            .ToListAsync(cancellationToken);

    public Task<bool> CorreoDuplicadoAsync(string correo, CancellationToken cancellationToken = default)
        => context.Admins.AnyAsync(a => a.Correo == correo);

    public async Task<AdminListItemDto> CreateAsync(string nombre, string correo, string hash, CancellationToken cancellationToken = default)
    {
        var admin = new Admin { Nombre = nombre, Correo = correo, Contrasena = hash };
        context.Admins.Add(admin);
        await context.SaveChangesAsync();
        return new AdminListItemDto { Id = admin.Id, Nombre = admin.Nombre, Correo = admin.Correo };
    }
}
