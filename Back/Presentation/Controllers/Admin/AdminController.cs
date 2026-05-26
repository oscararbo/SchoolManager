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
public class AdminController(IAdminService adminService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return this.ToActionResult(await adminService.GetAllAdminsAsync(HttpContext.RequestAborted));
    }

    [HttpGet("matriculas")]
    public async Task<IActionResult> GetMatriculas()
    {
        return this.ToActionResult(await adminService.GetMatriculasAsync(HttpContext.RequestAborted));
    }

    [HttpGet("imparticiones")]
    public async Task<IActionResult> GetImparticiones()
    {
        return this.ToActionResult(await adminService.GetImparticionesAsync(HttpContext.RequestAborted));
    }

    [HttpGet("horarios")]
    public async Task<IActionResult> GetHorarios()
    {
        return this.ToActionResult(await adminService.GetHorariosAsync(HttpContext.RequestAborted));
    }

    [HttpPost("horarios")]
    public async Task<IActionResult> CreateHorario([FromBody] CreateHorarioAsignaturaRequestDto requestDto)
    {
        return this.ToActionResult(await adminService.CreateHorarioAsync(requestDto, HttpContext.RequestAborted));
    }

    [HttpPut("horarios/{horarioId:int}")]
    public async Task<IActionResult> UpdateHorario(int horarioId, [FromBody] UpdateHorarioAsignaturaRequestDto requestDto)
    {
        return this.ToActionResult(await adminService.UpdateHorarioAsync(horarioId, requestDto, HttpContext.RequestAborted));
    }

    [HttpDelete("horarios/{horarioId:int}")]
    public async Task<IActionResult> DeleteHorario(int horarioId)
    {
        return this.ToActionResult(await adminService.DeleteHorarioAsync(horarioId, HttpContext.RequestAborted));
    }

    [HttpPost("create-admin")]
    public async Task<IActionResult> CreateAdmin(CreateAdminRequestDto createAdminRequestDto)
    {
        return this.ToActionResult(await adminService.CreateAdminAsync(createAdminRequestDto, User, HttpContext.RequestAborted));
    }
}

