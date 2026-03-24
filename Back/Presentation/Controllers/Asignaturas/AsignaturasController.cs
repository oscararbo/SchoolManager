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
public class AsignaturasController(IAsignaturasService asignaturasService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAll()
    {
        return this.ToActionResult(await asignaturasService.GetAllAsync(HttpContext.RequestAborted));
    }

    [HttpGet("simple")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetSimple()
    {
        var result = await asignaturasService.GetAllAsync(HttpContext.RequestAborted);
        if (result.Type != ApplicationResultType.Ok || result.Value is not IEnumerable<AsignaturaResumenDto> asignaturas)
        {
            return this.ToActionResult(result);
        }

        return Ok(asignaturas.Select(a => new
        {
            a.Id,
            a.Nombre,
            CursoId = a.Curso.Id,
            Curso = a.Curso.Nombre
        }));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetById(int id)
    {
        return this.ToActionResult(await asignaturasService.GetByIdAsync(id, HttpContext.RequestAborted));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create(CreateAsignaturaDto dto)
    {
        return this.ToActionResult(await asignaturasService.CreateAsync(dto, HttpContext.RequestAborted));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(int id, UpdateAsignaturaDto dto)
    {
        return this.ToActionResult(await asignaturasService.UpdateAsync(id, dto, HttpContext.RequestAborted));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        return this.ToActionResult(await asignaturasService.DeleteAsync(id, HttpContext.RequestAborted));
    }
}
