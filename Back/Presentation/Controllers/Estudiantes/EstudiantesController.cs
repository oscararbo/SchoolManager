using Back.Api.Application.Dtos;
using Back.Api.Application.Services;
using Back.Api.Application.Common;
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
        return this.ToActionResult(await estudiantesService.GetAllAsync(HttpContext.RequestAborted));
    }

    [HttpGet("simple")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetSimple()
    {
        var result = await estudiantesService.GetAllAsync(HttpContext.RequestAborted);
        if (result.Type != ApplicationResultType.Ok || result.Value is not IEnumerable<EstudianteListItemDto> estudiantes)
        {
            return this.ToActionResult(result);
        }

        return Ok(estudiantes.Select(e => new
        {
            e.Id,
            e.Nombre,
            e.CursoId,
            e.Curso
        }));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetById(int id)
    {
        return this.ToActionResult(await estudiantesService.GetByIdAsync(id, HttpContext.RequestAborted));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create(CreateEstudianteDto dto)
    {
        return this.ToActionResult(await estudiantesService.CreateAsync(dto, HttpContext.RequestAborted));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(int id, UpdateEstudianteDto dto)
    {
        return this.ToActionResult(await estudiantesService.UpdateAsync(id, dto, HttpContext.RequestAborted));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        return this.ToActionResult(await estudiantesService.DeleteAsync(id, HttpContext.RequestAborted));
    }

    [HttpPost("{id:int}/asignaturas/{asignaturaId:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Matricular(int id, int asignaturaId)
    {
        return this.ToActionResult(await estudiantesService.MatricularAsync(id, asignaturaId, HttpContext.RequestAborted));
    }

    [HttpDelete("{id:int}/asignaturas/{asignaturaId:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Desmatricular(int id, int asignaturaId)
    {
        return this.ToActionResult(await estudiantesService.DesmatricularAsync(id, asignaturaId, HttpContext.RequestAborted));
    }

    [HttpGet("{id:int}/panel")]
    [Authorize(Policy = "AlumnoOrAdmin")]
    public async Task<IActionResult> GetPanelAlumno(int id)
    {
        return this.ToActionResult(await estudiantesService.GetPanelAlumnoAsync(id, User, HttpContext.RequestAborted));
    }

    [HttpGet("{id:int}/panel-resumen")]
    [Authorize(Policy = "AlumnoOrAdmin")]
    public async Task<IActionResult> GetPanelResumen(int id)
    {
        return this.ToActionResult(await estudiantesService.GetPanelResumenAsync(id, User, HttpContext.RequestAborted));
    }

    [HttpGet("{id:int}/materias/{asignaturaId:int}/detalle")]
    [Authorize(Policy = "AlumnoOrAdmin")]
    public async Task<IActionResult> GetMateriaDetalle(int id, int asignaturaId)
    {
        return this.ToActionResult(await estudiantesService.GetMateriaDetalleAsync(id, asignaturaId, User, HttpContext.RequestAborted));
    }
}
