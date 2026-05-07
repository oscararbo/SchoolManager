using Back.Api.Application.Dtos;

namespace Back.Api.Application.Abstractions.Repositories;

public interface ISuperUsuarioDomainRepository
{
    Task<IEnumerable<ColegioListItemDto>> GetColegiosAsync(CancellationToken cancellationToken = default);
    Task<ColegioListItemDto?> GetColegioBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<ColegioListItemDto?> GetColegioByIdAsync(int colegioId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ColegioAdminListItemDto>> GetAdminsByColegioAsync(int colegioId, CancellationToken cancellationToken = default);
    Task<bool> ColegioSlugExistsAsync(string slug, int? exceptColegioId = null, CancellationToken cancellationToken = default);
    Task<ColegioListItemDto> CreateColegioAsync(string nombre, string slug, string? logoUrl, string? faviconUrl, string? colorPrimario, string? mensajeLogin, CancellationToken cancellationToken = default);
    Task<ColegioListItemDto?> UpdateColegioAsync(int colegioId, string nombre, string slug, string? logoUrl, string? faviconUrl, string? colorPrimario, string? mensajeLogin, CancellationToken cancellationToken = default);
    Task<bool> DeleteColegioAsync(int colegioId, CancellationToken cancellationToken = default);
    Task<ColegioAdminListItemDto> CreateAdminColegioAsync(int colegioId, string nombre, string correo, string contrasenaHash, CancellationToken cancellationToken = default);
    Task<bool> ColegioCorreoDuplicadoAsync(int colegioId, string correo, CancellationToken cancellationToken = default);
}
