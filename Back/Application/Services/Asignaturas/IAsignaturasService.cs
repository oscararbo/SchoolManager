using Back.Api.Application.Common;
using Back.Api.Application.Dtos;

namespace Back.Api.Application.Services;

public interface IAsignaturasService
{
    Task<ApplicationResult> GetAllAsignaturasAsync(CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetSimpleAsignaturasAsync(CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetAsignaturaByIdAsync(int asignaturaId, CancellationToken cancellationToken = default);
    Task<ApplicationResult> CreateAsignaturaAsync(CreateAsignaturaRequestDto createAsignaturaRequestDto, CancellationToken cancellationToken = default);
    Task<ApplicationResult> UpdateAsignaturaAsync(int asignaturaId, CreateAsignaturaRequestDto updateAsignaturaRequestDto, CancellationToken cancellationToken = default);
    Task<ApplicationResult> DeleteAsignaturaAsync(int asignaturaId, CancellationToken cancellationToken = default);
}
