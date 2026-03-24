using Back.Api.Application.Dtos;

namespace Back.Api.Domain.Repositories;

public interface IAdminDomainRepository
{
    Task<IEnumerable<AdminListItemDto>> GetAllAsync();
    Task<bool> CorreoDuplicadoAsync(string correo);
    Task<AdminListItemDto> CreateAsync(string nombre, string correo, string hash);
}
