using Back.Api.Dtos;
using Back.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AsignaturasController(IAsignaturasService asignaturasService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAll()
    {
        return await asignaturasService.GetAllAsync();
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetById(int id)
    {
        return await asignaturasService.GetByIdAsync(id);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create(CreateAsignaturaDto dto)
    {
        return await asignaturasService.CreateAsync(dto);
    }
}
