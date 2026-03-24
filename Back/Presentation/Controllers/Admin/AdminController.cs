using Back.Api.Application.Dtos;
using Back.Api.Application.Services;
using Back.Api.Presentation.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back.Api.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class AdminController(IAdminService adminService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return this.ToActionResult(await adminService.GetAllAsync(HttpContext.RequestAborted));
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        return this.ToActionResult(await adminService.GetStatsAsync(HttpContext.RequestAborted));
    }

        [HttpGet("stats/notas")]
        public async Task<IActionResult> GetNotasStats()
        {
            return this.ToActionResult(await adminService.GetNotasStatsAsync(HttpContext.RequestAborted));
        }

    [HttpPost("create-admin")]
    public async Task<IActionResult> CreateAdmin(CreateAdminDto dto)
    {
        return this.ToActionResult(await adminService.CreateAsync(dto, User, HttpContext.RequestAborted));
    }
}
