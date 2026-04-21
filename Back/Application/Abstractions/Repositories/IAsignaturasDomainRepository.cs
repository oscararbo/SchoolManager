using Back.Api.Application.Dtos;

namespace Back.Api.Application.Abstractions.Repositories;

public interface IAsignaturasDomainRepository
{
    Task<bool> ExisteAsync(int asignaturaId, CancellationToken cancellationToken = default);
    Task<bool> CursoExisteAsync(int cursoId, CancellationToken cancellationToken = default);
    Task<bool> ExisteEnCursoAsync(int cursoId, string nombre, CancellationToken cancellationToken = default);
    Task<string?> GetCursoNombreAsync(int cursoId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AsignaturaResumenDto>> GetAllResumenAsync(CancellationToken cancellationToken = default);
    Task<AsignaturaDetalleDto?> GetDetalleAsync(int asignaturaId, CancellationToken cancellationToken = default);
    Task<AsignaturaResumenDto> CreateAsignaturaAsync(string nombre, int cursoId, CancellationToken cancellationToken = default);
    Task<AsignaturaResumenDto?> UpdateAsignaturaAsync(int asignaturaId, string nombre, int cursoId, CancellationToken cancellationToken = default);
    Task DeleteAsignaturaAsync(int asignaturaId, CancellationToken cancellationToken = default);
}
