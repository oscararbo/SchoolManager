using Back.Api.Application.Common;
using Back.Api.Application.Dtos;
using Back.Api.Application.Dtos.SuperUsuario.Requests;

namespace Back.Api.Application.Services;

public interface ISuperUsuarioService
{
    Task<ApplicationResult> GetColegiosAsync(CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetColegioBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetAdminsByColegioAsync(int colegioId, CancellationToken cancellationToken = default);
    Task<ApplicationResult> CreateColegioAsync(CreateColegioRequestDto request, CancellationToken cancellationToken = default);
    Task<ApplicationResult> UpdateColegioAsync(int colegioId, UpdateColegioRequestDto request, CancellationToken cancellationToken = default);
    Task<ApplicationResult> DeleteColegioAsync(int colegioId, CancellationToken cancellationToken = default);
    Task<ApplicationResult> CreateAdminColegioAsync(int colegioId, CreateAdminColegioRequestDto request, CancellationToken cancellationToken = default);
    Task<ApplicationResult> UpdateColegioImagenAsync(int colegioId, UpdateColegioImagenRequestDto request, CancellationToken cancellationToken = default);
}
