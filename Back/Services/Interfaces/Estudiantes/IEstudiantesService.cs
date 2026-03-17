using Back.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Back.Api.Services;

public interface IEstudiantesService
{
    Task<IActionResult> GetAllAsync();
    Task<IActionResult> GetByIdAsync(int id);
    Task<IActionResult> CreateAsync(CreateEstudianteDto dto);
    Task<IActionResult> MatricularAsync(int id, int asignaturaId);
    Task<IActionResult> GetPanelAlumnoAsync(int id, ClaimsPrincipal user);
}
