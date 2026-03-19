using Back.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Back.Api.Services;

public interface IAdminService
{
    Task<IActionResult> GetAllAsync();
    Task<IActionResult> CreateAsync(CreateAdminDto dto, ClaimsPrincipal user);
}
