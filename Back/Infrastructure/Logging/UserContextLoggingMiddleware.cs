using Serilog.Context;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Back.Api.Infrastructure.Logging;

public class UserContextLoggingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correo = context.User.FindFirstValue(ClaimTypes.Email)
                    ?? context.User.FindFirstValue(JwtRegisteredClaimNames.Email)
                    ?? "anonymous";
        var userId = context.User.FindFirstValue("id")
                    ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? "-";
        var rol  = context.User.FindFirstValue(ClaimTypes.Role) ?? "-";
        var ip   = context.Connection.RemoteIpAddress?.ToString() ?? "-";

        using (correo != "anonymous" ? LogContext.PushProperty("UserEmail", correo) : null)
        using (userId != "-" ? LogContext.PushProperty("UserId", userId) : null)
        using (rol != "-" ? LogContext.PushProperty("UserRole", rol) : null)
        using (ip != "-" ? LogContext.PushProperty("ClientIp", ip) : null)
        {
            await next(context);
        }
    }
}
