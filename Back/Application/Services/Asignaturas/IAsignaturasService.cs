using Back.Api.Application.Common;
using Back.Api.Application.Dtos;

namespace Back.Api.Application.Services;

public interface IAsignaturasService
{
    Task<ApplicationResult> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApplicationResult> CreateAsync(CreateAsignaturaDto dto, CancellationToken cancellationToken = default);
    Task<ApplicationResult> UpdateAsync(int id, UpdateAsignaturaDto dto, CancellationToken cancellationToken = default);
    Task<ApplicationResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
