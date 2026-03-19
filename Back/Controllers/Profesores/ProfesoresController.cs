using Back.Api.Dtos;
using Back.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfesoresController(IProfesoresService profesoresService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAll()
    {
        return await profesoresService.GetAllAsync();
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetById(int id)
    {
        return await profesoresService.GetByIdAsync(id);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create(CreateProfesorDto dto)
    {
        return await profesoresService.CreateAsync(dto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(int id, UpdateProfesorDto dto)
    {
        return await profesoresService.UpdateAsync(id, dto);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        return await profesoresService.DeleteAsync(id);
    }

    [HttpGet("{id:int}/panel")]
    [Authorize(Policy = "ProfesorOrAdmin")]
    public async Task<IActionResult> GetPanelProfesor(int id)
    {
        return await profesoresService.GetPanelProfesorAsync(id, User);
    }

    [HttpGet("{profesorId:int}/asignaturas/{asignaturaId:int}/alumnos")]
    [Authorize(Policy = "ProfesorOrAdmin")]
    public async Task<IActionResult> GetAlumnosDeAsignatura(int profesorId, int asignaturaId)
    {
        return await profesoresService.GetAlumnosDeAsignaturaAsync(profesorId, asignaturaId, User);
    }

    [HttpPost("{profesorId:int}/imparticiones")]
    [Authorize(Policy = "ProfesorOrAdmin")]
    public async Task<IActionResult> AsignarImparticion(int profesorId, AsignarImparticionDto dto)
    {
        return await profesoresService.AsignarImparticionAsync(profesorId, dto, User);
    }

    [HttpPost("{profesorId:int}/notas")]
    [Authorize(Policy = "ProfesorOrAdmin")]
    public async Task<IActionResult> PonerNota(int profesorId, PonerNotaDto dto)
    {
        return await profesoresService.PonerNotaAsync(profesorId, dto, User);
    }

    [HttpPost("{profesorId:int}/tareas")]
    [Authorize(Policy = "ProfesorOrAdmin")]
    public async Task<IActionResult> CrearTarea(int profesorId, CreateTareaDto dto)
    {
        return await profesoresService.CrearTareaAsync(profesorId, dto, User);
    }

    [HttpGet("{profesorId:int}/asignaturas/{asignaturaId:int}/tareas")]
    [Authorize(Policy = "ProfesorOrAdmin")]
    public async Task<IActionResult> GetTareasDeAsignatura(int profesorId, int asignaturaId)
    {
        return await profesoresService.GetTareasDeAsignaturaAsync(profesorId, asignaturaId, User);
    }
}
