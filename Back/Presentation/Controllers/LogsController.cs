using Asp.Versioning;
using Back.Api.Application.Configuration;
using Back.Api.Application.Dtos;
using Back.Api.Application.Services.Audit;
using Back.Api.Presentation.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back.Api.Presentation.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.SuperUsuarioOnly)]
public class LogsController(IAuditLogService logsService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] LogsQueryRequest request)
    {
        return this.ToActionResult(
            await logsService.SearchAsync(request, HttpContext.RequestAborted)
        );
    }

    [HttpGet("timeline")]
    public async Task<IActionResult> Timeline([FromQuery] LogsQueryRequest request)
    {
        return this.ToActionResult(
            await logsService.GetTimelineAsync(request, HttpContext.RequestAborted)
        );
    }
}
