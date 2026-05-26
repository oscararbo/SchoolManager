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
[Route("api/admin/stats")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class AdminStatsController(IAdminStatsService adminStatsService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetStats()
    {
        return this.ToActionResult(await adminStatsService.GetStatsAsync(HttpContext.RequestAborted));
    }

    [HttpGet("top5")]
    public async Task<IActionResult> GetTop5()
    {
        return this.ToActionResult(await adminStatsService.GetTop5Async(HttpContext.RequestAborted));
    }

    [HttpGet("cursos")]
    public async Task<IActionResult> GetCursosStatsSelector()
    {
        return this.ToActionResult(await adminStatsService.GetCursosStatsSelectorAsync(HttpContext.RequestAborted));
    }

    [HttpGet("cursos/{cursoId:int}")]
    public async Task<IActionResult> GetStatsByCurso(int cursoId)
    {
        return this.ToActionResult(await adminStatsService.GetStatsByCursoAsync(cursoId, HttpContext.RequestAborted));
    }

    [HttpPost("cursos/comparar")]
    public async Task<IActionResult> CompareCursos(CompararCursosRequestDto compararCursosRequestDto)
    {
        return this.ToActionResult(await adminStatsService.CompareCursosAsync(compararCursosRequestDto.CursoIds, HttpContext.RequestAborted));
    }
}
