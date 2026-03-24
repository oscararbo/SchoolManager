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
        return this.ToActionResult(await adminService.GetAllAsync());
    }

    [HttpPost("create-admin")]
    public async Task<IActionResult> CreateAdmin(CreateAdminDto dto)
    {
        return this.ToActionResult(await adminService.CreateAsync(dto, User));
    }
}
