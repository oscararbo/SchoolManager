using Back.Api.Application.Dtos;
using Back.Api.Application.Services;
using Back.Api.Presentation.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back.Api.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfesoresController(IProfesoresService profesoresService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAll()
    {
        return this.ToActionResult(await profesoresService.GetAllAsync());
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetById(int id)
    {
        return this.ToActionResult(await profesoresService.GetByIdAsync(id));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create(CreateProfesorDto dto)
    {
        return this.ToActionResult(await profesoresService.CreateAsync(dto));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(int id, UpdateProfesorDto dto)
    {
        return this.ToActionResult(await profesoresService.UpdateAsync(id, dto));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        return this.ToActionResult(await profesoresService.DeleteAsync(id));
    }

    [HttpGet("{id:int}/panel")]
    [Authorize(Policy = "ProfesorOrAdmin")]
    public async Task<IActionResult> GetPanelProfesor(int id)
    {
        return this.ToActionResult(await profesoresService.GetPanelProfesorAsync(id, User));
    }

    [HttpGet("{profesorId:int}/asignaturas/{asignaturaId:int}/alumnos")]
    [Authorize(Policy = "ProfesorOrAdmin")]
    public async Task<IActionResult> GetAlumnosDeAsignatura(int profesorId, int asignaturaId)
    {
        return this.ToActionResult(await profesoresService.GetAlumnosDeAsignaturaAsync(profesorId, asignaturaId, User));
    }

    [HttpGet("{profesorId:int}/asignaturas/{asignaturaId:int}/alumnos-resumen")]
    [Authorize(Policy = "ProfesorOrAdmin")]
    public async Task<IActionResult> GetAlumnosResumenDeAsignatura(int profesorId, int asignaturaId)
    {
        return this.ToActionResult(await profesoresService.GetAlumnosResumenDeAsignaturaAsync(profesorId, asignaturaId, User));
    }

    [HttpGet("{profesorId:int}/asignaturas/{asignaturaId:int}/alumnos/{estudianteId:int}/detalle")]
    [Authorize(Policy = "ProfesorOrAdmin")]
    public async Task<IActionResult> GetAlumnoDetalleDeAsignatura(int profesorId, int asignaturaId, int estudianteId)
    {
        return this.ToActionResult(await profesoresService.GetAlumnoDetalleDeAsignaturaAsync(profesorId, asignaturaId, estudianteId, User));
    }

    [HttpGet("{profesorId:int}/asignaturas/{asignaturaId:int}/tareas/{tareaId:int}/calificaciones")]
    [Authorize(Policy = "ProfesorOrAdmin")]
    public async Task<IActionResult> GetCalificacionesDeTarea(int profesorId, int asignaturaId, int tareaId)
    {
        return this.ToActionResult(await profesoresService.GetCalificacionesDeTareaAsync(profesorId, asignaturaId, tareaId, User));
    }

    [HttpPost("{profesorId:int}/imparticiones")]
    [Authorize(Policy = "ProfesorOrAdmin")]
    public async Task<IActionResult> AsignarImparticion(int profesorId, AsignarImparticionDto dto)
    {
        return this.ToActionResult(await profesoresService.AsignarImparticionAsync(profesorId, dto, User));
    }

    [HttpPost("{profesorId:int}/notas")]
    [Authorize(Policy = "ProfesorOrAdmin")]
    public async Task<IActionResult> PonerNota(int profesorId, PonerNotaDto dto)
    {
        return this.ToActionResult(await profesoresService.PonerNotaAsync(profesorId, dto, User));
    }

    [HttpPost("{profesorId:int}/tareas")]
    [Authorize(Policy = "ProfesorOrAdmin")]
    public async Task<IActionResult> CrearTarea(int profesorId, CreateTareaDto dto)
    {
        return this.ToActionResult(await profesoresService.CrearTareaAsync(profesorId, dto, User));
    }

    [HttpGet("{profesorId:int}/asignaturas/{asignaturaId:int}/tareas")]
    [Authorize(Policy = "ProfesorOrAdmin")]
    public async Task<IActionResult> GetTareasDeAsignatura(int profesorId, int asignaturaId)
    {
        return this.ToActionResult(await profesoresService.GetTareasDeAsignaturaAsync(profesorId, asignaturaId, User));
    }

    [HttpGet("asignaturas/{asignaturaId:int}/tareas-notas")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetTareasConNotas(int asignaturaId)
    {
        return this.ToActionResult(await profesoresService.GetTareasConNotasAsync(asignaturaId, User));
    }
}
