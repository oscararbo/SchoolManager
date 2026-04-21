using Back.Api.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Back.Api.Presentation.Http;

public static class ApplicationResultExtensions
{
    public static IActionResult ToActionResult(this ControllerBase controller, ApplicationResult applicationResult)
    {
        return applicationResult.Type switch
        {
            ApplicationResultType.Ok => applicationResult.Value is null ? controller.Ok() : controller.Ok(applicationResult.Value),
            ApplicationResultType.Created => controller.Created(applicationResult.Location ?? string.Empty, applicationResult.Value),
            ApplicationResultType.NoContent => controller.NoContent(),
            ApplicationResultType.BadRequest => applicationResult.Value is null ? controller.BadRequest() : controller.BadRequest(applicationResult.Value),
            ApplicationResultType.Unauthorized => applicationResult.Value is null ? controller.Unauthorized() : controller.Unauthorized(applicationResult.Value),
            ApplicationResultType.Forbidden => applicationResult.Value is null
                ? controller.Forbid()
                : controller.StatusCode(StatusCodes.Status403Forbidden, applicationResult.Value),
            ApplicationResultType.NotFound => applicationResult.Value is null ? controller.NotFound() : controller.NotFound(applicationResult.Value),
            _ => controller.StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
