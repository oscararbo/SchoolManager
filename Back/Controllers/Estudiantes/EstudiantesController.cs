using Back.Api.Dtos;
using Back.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EstudiantesController(IEstudiantesService estudiantesService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAll()
    {
        return await estudiantesService.GetAllAsync();
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetById(int id)
    {
        return await estudiantesService.GetByIdAsync(id);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create(CreateEstudianteDto dto)
    {
        return await estudiantesService.CreateAsync(dto);
    }

    [HttpPost("{id:int}/asignaturas/{asignaturaId:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Matricular(int id, int asignaturaId)
    {
        return await estudiantesService.MatricularAsync(id, asignaturaId);
    }

    [HttpGet("{id:int}/panel")]
    [Authorize(Policy = "AlumnoOrAdmin")]
    public async Task<IActionResult> GetPanelAlumno(int id)
    {
        return await estudiantesService.GetPanelAlumnoAsync(id, User);
    }
}
