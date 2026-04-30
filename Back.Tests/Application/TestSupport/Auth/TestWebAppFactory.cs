using System.Security.Claims;
using System.Text.Encodings.Web;
using Back.Api.Application.Configuration;
using Back.Api.Persistence.Context;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Back.Tests.Application.TestSupport.Auth;

public sealed class TestWebAppFactory : WebApplicationFactory<Program>
{
    public const string TestScheme = "TestScheme";

    protected override void ConfigureWebHost(IWebHostBuilder webHostBuilder)
    {
        webHostBuilder.UseEnvironment("Testing");

        webHostBuilder.ConfigureServices(serviceCollection =>
        {
            serviceCollection.RemoveAll(typeof(IDbContextOptionsConfiguration<AppDbContext>));
            serviceCollection.RemoveAll(typeof(DbContextOptions));
            serviceCollection.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            serviceCollection.RemoveAll(typeof(AppDbContext));
            serviceCollection.RemoveAll(typeof(IDbContextFactory<AppDbContext>));

            var inMemoryDatabaseName = $"BackTests_{Guid.NewGuid()}";

            serviceCollection.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(inMemoryDatabaseName));
            serviceCollection.AddDbContextFactory<AppDbContext>(options => options.UseInMemoryDatabase(inMemoryDatabaseName));

            serviceCollection
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestScheme;
                    options.DefaultChallengeScheme = TestScheme;
                    options.DefaultScheme = TestScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestScheme, _ => { });
        });
    }
}

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string RoleHeader = "X-Test-Role";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authorizationHeaderValue) ||
            !authorizationHeaderValue.ToString().StartsWith(TestWebAppFactory.TestScheme, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var userRole = Request.Headers.TryGetValue(RoleHeader, out var roleHeaderValues)
            ? roleHeaderValues.ToString()
            : Roles.Alumno;

        var userClaims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "test-user"),
            new(ClaimTypes.Name, "Usuario Test"),
            new(ClaimTypes.Role, userRole)
        };

        var identity = new ClaimsIdentity(userClaims, TestWebAppFactory.TestScheme);
        var principal = new ClaimsPrincipal(identity);
        var authenticationTicket = new AuthenticationTicket(principal, TestWebAppFactory.TestScheme);

        return Task.FromResult(AuthenticateResult.Success(authenticationTicket));
    }
}