using Back.Api.Application.Common;
using Back.Api.Application.Dtos;

namespace Back.Api.Application.Services;

public interface ICursosService
{
    Task<ApplicationResult> GetAllAsync();
    Task<ApplicationResult> GetByIdAsync(int id);
    Task<ApplicationResult> CreateAsync(CreateCursoDto dto);
    Task<ApplicationResult> UpdateAsync(int id, UpdateCursoDto dto);
    Task<ApplicationResult> DeleteAsync(int id);
}
