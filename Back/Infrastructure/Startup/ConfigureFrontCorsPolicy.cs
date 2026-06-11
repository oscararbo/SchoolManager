using Back.Api.Application.Configuration;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace Back.Api.Infrastructure.Startup;

public sealed class ConfigureFrontCorsPolicy(IOptions<FrontCorsOptions> frontCorsOptions)
    : IConfigureOptions<CorsOptions>
{
    public void Configure(CorsOptions options)
    {
        var allowedOrigins = frontCorsOptions.Value.AllowedOrigins;

        options.AddPolicy("Front", policy =>
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials());
    }
}
