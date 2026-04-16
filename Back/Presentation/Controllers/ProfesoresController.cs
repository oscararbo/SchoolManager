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
public class ProfesoresController(IProfesoresService profesoresService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> GetAll()
    {
        return this.ToActionResult(await profesoresService.GetAllAsync(HttpContext.RequestAborted));
    }

    [HttpGet("simple")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> GetSimple()
    {
        return this.ToActionResult(await profesoresService.GetSimpleAsync(HttpContext.RequestAborted));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> GetById(int id)
    {
        return this.ToActionResult(await profesoresService.GetByIdAsync(id, HttpContext.RequestAborted));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Create(CreateProfesorRequestDto dto)
    {
        return this.ToActionResult(await profesoresService.CreateAsync(dto, HttpContext.RequestAborted));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Update(int id, UpdateProfesorRequestDto dto)
    {
        return this.ToActionResult(await profesoresService.UpdateAsync(id, dto, HttpContext.RequestAborted));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(int id)
    {
        return this.ToActionResult(await profesoresService.DeleteAsync(id, HttpContext.RequestAborted));
    }

    [HttpGet("{id:int}/panel")]
    [Authorize(Policy = AuthorizationPolicies.ProfesorOrAdmin)]
    public async Task<IActionResult> GetPanelProfesor(int id)
    {
        return this.ToActionResult(await profesoresService.GetPanelProfesorAsync(id, User, HttpContext.RequestAborted));
    }

    [HttpGet("{profesorId:int}/asignaturas/{asignaturaId:int}/alumnos")]
    [Authorize(Policy = AuthorizationPolicies.ProfesorOrAdmin)]
    public async Task<IActionResult> GetAlumnosDeAsignatura(int profesorId, int asignaturaId)
    {
        return this.ToActionResult(await profesoresService.GetAlumnosDeAsignaturaAsync(profesorId, asignaturaId, User, HttpContext.RequestAborted));
    }

    [HttpGet("{profesorId:int}/asignaturas/{asignaturaId:int}/alumnos-resumen")]
    [Authorize(Policy = AuthorizationPolicies.ProfesorOrAdmin)]
    public async Task<IActionResult> GetAlumnosResumenDeAsignatura(int profesorId, int asignaturaId)
    {
        return this.ToActionResult(await profesoresService.GetAlumnosResumenDeAsignaturaAsync(profesorId, asignaturaId, User, HttpContext.RequestAborted));
    }

    [HttpGet("{profesorId:int}/asignaturas/{asignaturaId:int}/alumnos/{estudianteId:int}/detalle")]
    [Authorize(Policy = AuthorizationPolicies.ProfesorOrAdmin)]
    public async Task<IActionResult> GetAlumnoDetalleDeAsignatura(int profesorId, int asignaturaId, int estudianteId)
    {
        return this.ToActionResult(await profesoresService.GetAlumnoDetalleDeAsignaturaAsync(profesorId, asignaturaId, estudianteId, User, HttpContext.RequestAborted));
    }

    [HttpGet("{profesorId:int}/asignaturas/{asignaturaId:int}/tareas/{tareaId:int}/calificaciones")]
    [Authorize(Policy = AuthorizationPolicies.ProfesorOrAdmin)]
    public async Task<IActionResult> GetCalificacionesDeTarea(int profesorId, int asignaturaId, int tareaId)
    {
        return this.ToActionResult(await profesoresService.GetCalificacionesDeTareaAsync(profesorId, asignaturaId, tareaId, User, HttpContext.RequestAborted));
    }

    [HttpPost("{profesorId:int}/imparticiones")]
    [Authorize(Policy = AuthorizationPolicies.ProfesorOrAdmin)]
    public async Task<IActionResult> AsignarImparticion(int profesorId, AsignarImparticionRequestDto dto)
    {
        return this.ToActionResult(await profesoresService.AsignarImparticionAsync(profesorId, dto, User, HttpContext.RequestAborted));
    }

    [HttpDelete("{profesorId:int}/imparticiones/{asignaturaId:int}/{cursoId:int}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> EliminarImparticion(int profesorId, int asignaturaId, int cursoId)
    {
        return this.ToActionResult(await profesoresService.EliminarImparticionAsync(profesorId, asignaturaId, cursoId, User, HttpContext.RequestAborted));
    }

    [HttpPost("{profesorId:int}/notas")]
    [Authorize(Policy = AuthorizationPolicies.ProfesorOrAdmin)]
    public async Task<IActionResult> PonerNota(int profesorId, PonerNotaRequestDto dto)
    {
        return this.ToActionResult(await profesoresService.PonerNotaAsync(profesorId, dto, User, HttpContext.RequestAborted));
    }

    [HttpPost("{profesorId:int}/tareas")]
    [Authorize(Policy = AuthorizationPolicies.ProfesorOrAdmin)]
    public async Task<IActionResult> CrearTarea(int profesorId, CreateTareaRequestDto dto)
    {
        return this.ToActionResult(await profesoresService.CrearTareaAsync(profesorId, dto, User, HttpContext.RequestAborted));
    }

    [HttpGet("{profesorId:int}/asignaturas/{asignaturaId:int}/tareas")]
    [Authorize(Policy = AuthorizationPolicies.ProfesorOrAdmin)]
    public async Task<IActionResult> GetTareasDeAsignatura(int profesorId, int asignaturaId)
    {
        return this.ToActionResult(await profesoresService.GetTareasDeAsignaturaAsync(profesorId, asignaturaId, User, HttpContext.RequestAborted));
    }

    [HttpGet("asignaturas/{asignaturaId:int}/tareas-notas")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> GetTareasConNotas(int asignaturaId)
    {
        return this.ToActionResult(await profesoresService.GetTareasConNotasAsync(asignaturaId, User, HttpContext.RequestAborted));
    }

    [HttpGet("{profesorId:int}/stats")]
    [Authorize(Policy = AuthorizationPolicies.ProfesorOrAdmin)]
    public async Task<IActionResult> GetStats(int profesorId)
    {
        return this.ToActionResult(await profesoresService.GetStatsAsync(profesorId, User, HttpContext.RequestAborted));
    }
}


