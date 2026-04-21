using Back.Api.Application.Dtos;
using Back.Api.Application.Configuration;
using Back.Api.Application.Services;
using Back.Api.Presentation.Http;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back.Api.Presentation.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
[Authorize]
public class EstudiantesController(IEstudiantesService estudiantesService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> GetAll()
    {
        return this.ToActionResult(await estudiantesService.GetAllEstudiantesAsync(HttpContext.RequestAborted));
    }

    [HttpGet("simple")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> GetSimple()
    {
        return this.ToActionResult(await estudiantesService.GetSimpleEstudiantesAsync(HttpContext.RequestAborted));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> GetById(int id)
    {
        return this.ToActionResult(await estudiantesService.GetEstudianteByIdAsync(id, HttpContext.RequestAborted));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Create(CreateEstudianteRequestDto createEstudianteRequestDto)
    {
        return this.ToActionResult(await estudiantesService.CreateEstudianteAsync(createEstudianteRequestDto, HttpContext.RequestAborted));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Update(int id, UpdateEstudianteRequestDto updateEstudianteRequestDto)
    {
        return this.ToActionResult(await estudiantesService.UpdateEstudianteAsync(id, updateEstudianteRequestDto, HttpContext.RequestAborted));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(int id)
    {
        return this.ToActionResult(await estudiantesService.DeleteEstudianteAsync(id, HttpContext.RequestAborted));
    }

    [HttpPost("{id:int}/asignaturas/{asignaturaId:int}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Matricular(int id, int asignaturaId)
    {
        return this.ToActionResult(await estudiantesService.MatricularAsync(id, asignaturaId, HttpContext.RequestAborted));
    }

    [HttpDelete("{id:int}/asignaturas/{asignaturaId:int}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Desmatricular(int id, int asignaturaId)
    {
        return this.ToActionResult(await estudiantesService.DesmatricularAsync(id, asignaturaId, HttpContext.RequestAborted));
    }

    [HttpGet("{id:int}/panel")]
    [Authorize(Policy = AuthorizationPolicies.AlumnoOrAdmin)]
    public async Task<IActionResult> GetPanelAlumno(int id)
    {
        return this.ToActionResult(await estudiantesService.GetPanelAlumnoAsync(id, User, HttpContext.RequestAborted));
    }

    [HttpGet("{id:int}/panel-resumen")]
    [Authorize(Policy = AuthorizationPolicies.AlumnoOrAdmin)]
    public async Task<IActionResult> GetPanelResumen(int id)
    {
        return this.ToActionResult(await estudiantesService.GetPanelResumenAsync(id, User, HttpContext.RequestAborted));
    }

    [HttpGet("{id:int}/materias/{asignaturaId:int}/detalle")]
    [Authorize(Policy = AuthorizationPolicies.AlumnoOrAdmin)]
    public async Task<IActionResult> GetMateriaDetalle(int id, int asignaturaId)
    {
        return this.ToActionResult(await estudiantesService.GetMateriaDetalleAsync(id, asignaturaId, User, HttpContext.RequestAborted));
    }
}

