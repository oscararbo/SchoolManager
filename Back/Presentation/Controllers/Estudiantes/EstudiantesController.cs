using Back.Api.Application.Dtos;
using Back.Api.Application.Services;
using Back.Api.Presentation.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back.Api.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EstudiantesController(IEstudiantesService estudiantesService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAll()
    {
        return this.ToActionResult(await estudiantesService.GetAllAsync());
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetById(int id)
    {
        return this.ToActionResult(await estudiantesService.GetByIdAsync(id));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create(CreateEstudianteDto dto)
    {
        return this.ToActionResult(await estudiantesService.CreateAsync(dto));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(int id, UpdateEstudianteDto dto)
    {
        return this.ToActionResult(await estudiantesService.UpdateAsync(id, dto));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        return this.ToActionResult(await estudiantesService.DeleteAsync(id));
    }

    [HttpPost("{id:int}/asignaturas/{asignaturaId:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Matricular(int id, int asignaturaId)
    {
        return this.ToActionResult(await estudiantesService.MatricularAsync(id, asignaturaId));
    }

    [HttpGet("{id:int}/panel")]
    [Authorize(Policy = "AlumnoOrAdmin")]
    public async Task<IActionResult> GetPanelAlumno(int id)
    {
        return this.ToActionResult(await estudiantesService.GetPanelAlumnoAsync(id, User));
    }

    [HttpGet("{id:int}/panel-resumen")]
    [Authorize(Policy = "AlumnoOrAdmin")]
    public async Task<IActionResult> GetPanelResumen(int id)
    {
        return this.ToActionResult(await estudiantesService.GetPanelResumenAsync(id, User));
    }

    [HttpGet("{id:int}/materias/{asignaturaId:int}/detalle")]
    [Authorize(Policy = "AlumnoOrAdmin")]
    public async Task<IActionResult> GetMateriaDetalle(int id, int asignaturaId)
    {
        return this.ToActionResult(await estudiantesService.GetMateriaDetalleAsync(id, asignaturaId, User));
    }
}
