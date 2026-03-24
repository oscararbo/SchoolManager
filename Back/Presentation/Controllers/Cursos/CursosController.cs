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
public class CursosController(ICursosService cursosService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAll()
    {
        return this.ToActionResult(await cursosService.GetAllAsync(HttpContext.RequestAborted));
    }

    [HttpGet("simple")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetSimple()
    {
        var result = await cursosService.GetAllAsync(HttpContext.RequestAborted);
        if (result.Type != ApplicationResultType.Ok || result.Value is not IEnumerable<CursoResumenDto> cursos)
        {
            return this.ToActionResult(result);
        }

        return Ok(cursos.Select(c => new CursoSimpleDto
        {
            Id = c.Id,
            Nombre = c.Nombre
        }));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetById(int id)
    {
        return this.ToActionResult(await cursosService.GetByIdAsync(id, HttpContext.RequestAborted));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create(CreateCursoDto dto)
    {
        return this.ToActionResult(await cursosService.CreateAsync(dto, HttpContext.RequestAborted));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(int id, UpdateCursoDto dto)
    {
        return this.ToActionResult(await cursosService.UpdateAsync(id, dto, HttpContext.RequestAborted));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        return this.ToActionResult(await cursosService.DeleteAsync(id, HttpContext.RequestAborted));
    }
}
