using Back.Api.Dtos;
using Back.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CursosController(ICursosService cursosService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAll()
    {
        return await cursosService.GetAllAsync();
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetById(int id)
    {
        return await cursosService.GetByIdAsync(id);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create(CreateCursoDto dto)
    {
        return await cursosService.CreateAsync(dto);
    }
}
