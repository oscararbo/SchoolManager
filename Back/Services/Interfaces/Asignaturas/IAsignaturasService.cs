using Back.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Back.Api.Services;

public interface IAsignaturasService
{
    Task<IActionResult> GetAllAsync();
    Task<IActionResult> GetByIdAsync(int id);
    Task<IActionResult> CreateAsync(CreateAsignaturaDto dto);
}
