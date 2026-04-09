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
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class AsignaturasController(IAsignaturasService asignaturasService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        return this.ToActionResult(await asignaturasService.GetAllAsync(HttpContext.RequestAborted));
    }

    [HttpGet("simple")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSimple()
    {
        return this.ToActionResult(await asignaturasService.GetSimpleAsync(HttpContext.RequestAborted));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        return this.ToActionResult(await asignaturasService.GetByIdAsync(id, HttpContext.RequestAborted));
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateAsignaturaRequestDto dto)
    {
        return this.ToActionResult(await asignaturasService.CreateAsync(dto, HttpContext.RequestAborted));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, CreateAsignaturaRequestDto dto)
    {
        return this.ToActionResult(await asignaturasService.UpdateAsync(id, dto, HttpContext.RequestAborted));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        return this.ToActionResult(await asignaturasService.DeleteAsync(id, HttpContext.RequestAborted));
    }
}
