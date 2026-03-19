using Back.Api.Dtos;
using Back.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class AdminController(IAdminService adminService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return await adminService.GetAllAsync();
    }

    [HttpPost("create-admin")]
    public async Task<IActionResult> CreateAdmin(CreateAdminDto dto)
    {
        return await adminService.CreateAsync(dto, User);
    }
}
