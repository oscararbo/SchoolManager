using Back.Api.Application.Common;
using Back.Api.Application.Dtos;

namespace Back.Api.Application.Services;

public interface ICursosService
{
    Task<ApplicationResult> GetAllCursosAsync(CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetSimpleCursosAsync(CancellationToken cancellationToken = default);
    Task<ApplicationResult> GetCursoByIdAsync(int cursoId, CancellationToken cancellationToken = default);
    Task<ApplicationResult> CreateCursoAsync(CreateCursoRequestDto createCursoRequestDto, CancellationToken cancellationToken = default);
    Task<ApplicationResult> UpdateCursoAsync(int cursoId, CreateCursoRequestDto updateCursoRequestDto, CancellationToken cancellationToken = default);
    Task<ApplicationResult> DeleteCursoAsync(int cursoId, CancellationToken cancellationToken = default);
}
