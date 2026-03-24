using Back.Api.Application.Common;
using Back.Api.Application.Dtos;

namespace Back.Api.Application.Services;

public interface IAsignaturasService
{
    Task<ApplicationResult> GetAllAsync();
    Task<ApplicationResult> GetByIdAsync(int id);
    Task<ApplicationResult> CreateAsync(CreateAsignaturaDto dto);
    Task<ApplicationResult> UpdateAsync(int id, UpdateAsignaturaDto dto);
    Task<ApplicationResult> DeleteAsync(int id);
}
