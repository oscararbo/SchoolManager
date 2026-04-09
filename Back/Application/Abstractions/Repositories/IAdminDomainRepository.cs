using Back.Api.Application.Dtos;

namespace Back.Api.Application.Abstractions.Repositories;

public interface IAdminDomainRepository
{
    Task<IEnumerable<AdminListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> CorreoDuplicadoAsync(string correo, CancellationToken cancellationToken = default);
    Task<AdminListItemDto> CreateAsync(string nombre, string correo, string hash, CancellationToken cancellationToken = default);
    Task<AdminStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<CursoStatsSelectorDto>> GetCursosStatsSelectorAsync(CancellationToken cancellationToken = default);
    Task<CursoNotasStatsResponseDto?> GetStatsByCursoAsync(int cursoId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CursoComparacionItemDto>> CompareCursosAsync(IEnumerable<int> cursoIds, CancellationToken cancellationToken = default);
    Task<IEnumerable<AdminMatriculaListReadModelDto>> GetMatriculasAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<AdminImparticionListReadModelDto>> GetImparticionesAsync(CancellationToken cancellationToken = default);
}