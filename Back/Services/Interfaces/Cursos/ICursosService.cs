using Back.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Back.Api.Services;

public interface ICursosService
{
    Task<IActionResult> GetAllAsync();
    Task<IActionResult> GetByIdAsync(int id);
    Task<IActionResult> CreateAsync(CreateCursoDto dto);
    Task<IActionResult> UpdateAsync(int id, UpdateCursoDto dto);
    Task<IActionResult> DeleteAsync(int id);
}
