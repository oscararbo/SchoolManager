using Back.Api.Application.Dtos;
using Back.Api.Application.Configuration;
using Back.Api.Application.Services;
using Back.Api.Presentation.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back.Api.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class AsignaturasController(IAsignaturasService asignaturasService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return this.ToActionResult(await asignaturasService.GetAllAsync(HttpContext.RequestAborted));
    }

    [HttpGet("simple")]
    public async Task<IActionResult> GetSimple()
    {
        return this.ToActionResult(await asignaturasService.GetSimpleAsync(HttpContext.RequestAborted));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        return this.ToActionResult(await asignaturasService.GetByIdAsync(id, HttpContext.RequestAborted));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAsignaturaRequestDto dto)
    {
        return this.ToActionResult(await asignaturasService.CreateAsync(dto, HttpContext.RequestAborted));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CreateAsignaturaRequestDto dto)
    {
        return this.ToActionResult(await asignaturasService.UpdateAsync(id, dto, HttpContext.RequestAborted));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        return this.ToActionResult(await asignaturasService.DeleteAsync(id, HttpContext.RequestAborted));
    }
}
