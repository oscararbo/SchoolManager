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

    [HttpPost("create-admin")]
    public async Task<IActionResult> CreateAdmin(CreateAdminRequestDto createAdminRequestDto)
    {
        return this.ToActionResult(await adminService.CreateAdminAsync(createAdminRequestDto, User, HttpContext.RequestAborted));
    }
}

