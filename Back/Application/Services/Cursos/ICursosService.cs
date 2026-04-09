using Back.Api.Application.Common;
using Back.Api.Application.Dtos;

namespace Back.Api.Application.Services;

public interface ICursosService
{
    Task<ApplicationResult> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetSimpleAsync(CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApplicationResult> CreateAsync(CreateCursoRequestDto dto, CancellationToken cancellationToken = default);
    Task<ApplicationResult> UpdateAsync(int id, CreateCursoRequestDto dto, CancellationToken cancellationToken = default);
    Task<ApplicationResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
