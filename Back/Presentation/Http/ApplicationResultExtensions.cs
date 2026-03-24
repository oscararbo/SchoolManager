using Back.Api.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Back.Api.Presentation.Http;

public static class ApplicationResultExtensions
{
    public static IActionResult ToActionResult(this ControllerBase controller, ApplicationResult result)
    {
        return result.Type switch
        {
            ApplicationResultType.Ok => result.Value is null ? controller.Ok() : controller.Ok(result.Value),
            ApplicationResultType.Created => controller.Created(result.Location ?? string.Empty, result.Value),
            ApplicationResultType.NoContent => controller.NoContent(),
            ApplicationResultType.BadRequest => result.Value is null ? controller.BadRequest() : controller.BadRequest(result.Value),
            ApplicationResultType.Unauthorized => result.Value is null ? controller.Unauthorized() : controller.Unauthorized(result.Value),
            ApplicationResultType.Forbidden => controller.Forbid(),
            ApplicationResultType.NotFound => result.Value is null ? controller.NotFound() : controller.NotFound(result.Value),
            _ => controller.StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
