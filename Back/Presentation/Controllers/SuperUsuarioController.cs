using Asp.Versioning;
using Back.Api.Application.Configuration;
using Back.Api.Application.Dtos;
using Back.Api.Application.Services;
using Back.Api.Presentation.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back.Api.Presentation.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
public class SuperUsuarioController(ISuperUsuarioService superUsuarioService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("colegios/slug/{slug}")]
    public async Task<IActionResult> GetColegioBySlug(string slug)
        => this.ToActionResult(await superUsuarioService.GetColegioBySlugAsync(slug, HttpContext.RequestAborted));

    [Authorize(Policy = AuthorizationPolicies.SuperUsuarioOnly)]
    [HttpGet("colegios")]
    public async Task<IActionResult> GetColegios()
        => this.ToActionResult(await superUsuarioService.GetColegiosAsync(HttpContext.RequestAborted));

    [Authorize(Policy = AuthorizationPolicies.SuperUsuarioOnly)]
    [HttpGet("colegios/{colegioId:int}/admins")]
    public async Task<IActionResult> GetAdminsByColegio(int colegioId)
        => this.ToActionResult(await superUsuarioService.GetAdminsByColegioAsync(colegioId, HttpContext.RequestAborted));

    [Authorize(Policy = AuthorizationPolicies.SuperUsuarioOnly)]
    [HttpPost("colegios")]
    public async Task<IActionResult> CreateColegio(CreateColegioRequestDto request)
        => this.ToActionResult(await superUsuarioService.CreateColegioAsync(request, HttpContext.RequestAborted));

    [Authorize(Policy = AuthorizationPolicies.SuperUsuarioOnly)]
    [HttpPut("colegios/{colegioId:int}")]
    public async Task<IActionResult> UpdateColegio(int colegioId, UpdateColegioRequestDto request)
        => this.ToActionResult(await superUsuarioService.UpdateColegioAsync(colegioId, request, HttpContext.RequestAborted));

    [Authorize(Policy = AuthorizationPolicies.SuperUsuarioOnly)]
    [HttpDelete("colegios/{colegioId:int}")]
    public async Task<IActionResult> DeleteColegio(int colegioId)
        => this.ToActionResult(await superUsuarioService.DeleteColegioAsync(colegioId, HttpContext.RequestAborted));

    [Authorize(Policy = AuthorizationPolicies.SuperUsuarioOnly)]
    [HttpPost("colegios/{colegioId:int}/admins")]
    public async Task<IActionResult> CreateAdminColegio(int colegioId, CreateAdminColegioRequestDto request)
        => this.ToActionResult(await superUsuarioService.CreateAdminColegioAsync(colegioId, request, HttpContext.RequestAborted));
}
