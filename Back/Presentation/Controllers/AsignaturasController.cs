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
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class AsignaturasController(IAsignaturasService asignaturasService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return this.ToActionResult(await asignaturasService.GetAllAsignaturasAsync(HttpContext.RequestAborted));
    }

    [HttpGet("simple")]
    public async Task<IActionResult> GetSimple()
    {
        return this.ToActionResult(await asignaturasService.GetSimpleAsignaturasAsync(HttpContext.RequestAborted));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        return this.ToActionResult(await asignaturasService.GetAsignaturaByIdAsync(id, HttpContext.RequestAborted));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAsignaturaRequestDto createAsignaturaRequestDto)
    {
        return this.ToActionResult(await asignaturasService.CreateAsignaturaAsync(createAsignaturaRequestDto, HttpContext.RequestAborted));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CreateAsignaturaRequestDto updateAsignaturaRequestDto)
    {
        return this.ToActionResult(await asignaturasService.UpdateAsignaturaAsync(id, updateAsignaturaRequestDto, HttpContext.RequestAborted));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        return this.ToActionResult(await asignaturasService.DeleteAsignaturaAsync(id, HttpContext.RequestAborted));
    }
}

