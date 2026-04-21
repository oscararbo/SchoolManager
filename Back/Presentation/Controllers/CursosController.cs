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
public class CursosController(ICursosService cursosService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return this.ToActionResult(await cursosService.GetAllCursosAsync(HttpContext.RequestAborted));
    }

    [HttpGet("simple")]
    public async Task<IActionResult> GetSimple()
    {
        return this.ToActionResult(await cursosService.GetSimpleCursosAsync(HttpContext.RequestAborted));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        return this.ToActionResult(await cursosService.GetCursoByIdAsync(id, HttpContext.RequestAborted));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCursoRequestDto createCursoRequestDto)
    {
        return this.ToActionResult(await cursosService.CreateCursoAsync(createCursoRequestDto, HttpContext.RequestAborted));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CreateCursoRequestDto updateCursoRequestDto)
    {
        return this.ToActionResult(await cursosService.UpdateCursoAsync(id, updateCursoRequestDto, HttpContext.RequestAborted));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        return this.ToActionResult(await cursosService.DeleteCursoAsync(id, HttpContext.RequestAborted));
    }
}

