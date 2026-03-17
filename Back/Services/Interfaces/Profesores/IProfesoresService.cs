using Back.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Back.Api.Services;

public interface IProfesoresService
{
    Task<IActionResult> GetAllAsync();
    Task<IActionResult> GetByIdAsync(int id);
    Task<IActionResult> CreateAsync(CreateProfesorDto dto);
    Task<IActionResult> GetPanelProfesorAsync(int id, ClaimsPrincipal user);
    Task<IActionResult> GetAlumnosDeAsignaturaAsync(int profesorId, int asignaturaId, ClaimsPrincipal user);
    Task<IActionResult> AsignarImparticionAsync(int profesorId, AsignarImparticionDto dto, ClaimsPrincipal user);
    Task<IActionResult> PonerNotaAsync(int profesorId, PonerNotaDto dto, ClaimsPrincipal user);
    Task<IActionResult> CrearTareaAsync(int profesorId, CreateTareaDto dto, ClaimsPrincipal user);
    Task<IActionResult> GetTareasDeAsignaturaAsync(int profesorId, int asignaturaId, ClaimsPrincipal user);
}
