using Back.Api.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Back.Tests.Integration;

/// <summary>
/// Factory that replaces the PostgreSQL DbContext with an in-memory one and
/// replaces JWT authentication with a test scheme driven by the X-Test-Role header.
/// </summary>
public class WebAppFactory : WebApplicationFactory<Program>
{
    // Unique database name per factory instance so tests don't share state.
    private readonly string _dbName = $"IntegrationTest_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Provide settings explicitly so the factory never depends on
        // the content-root discovery finding Back/appsettings.json.
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"]                  = "ClaveSuperSecretaMuyLargaParaJwtHS256_2026_Segura",
                ["Jwt:Issuer"]               = "Back.Api",
                ["Jwt:Audience"]             = "Front.App",
                ["Jwt:ExpiresMinutes"]       = "120",
                ["Jwt:RefreshExpiresDays"]   = "7",
                ["SeedAdmin:Nombre"]         = "Administrador",
                ["SeedAdmin:Correo"]         = "admin@prueba.com",
                ["SeedAdmin:Contrasena"]     = "Prueba1",
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=localhost;Port=5432;Database=testdb;Username=postgres;Password=postgres"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // ── Replace PostgreSQL with InMemory ──────────────────────────
            // EF Core 8+ registers IDbContextOptionsConfiguration<TContext> alongside
            // DbContextOptions<TContext>. We must remove both to prevent
            // "two database providers registered" runtime errors.
            var descriptorsToRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>) &&
                     d.ServiceType.GenericTypeArguments[0] == typeof(AppDbContext)))
                .ToList();


            foreach (var d in descriptorsToRemove)
                services.Remove(d);

            services.AddDbContext<AppDbContext>(opts =>
                opts.UseInMemoryDatabase(_dbName));

            // ── Replace JWT auth with a header-driven test scheme ─────────
            services.AddAuthentication("TestScheme")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    "TestScheme", _ => { });

            services.PostConfigureAll<AuthenticationOptions>(opts =>
            {
                opts.DefaultAuthenticateScheme = "TestScheme";
                opts.DefaultChallengeScheme    = "TestScheme";
                opts.DefaultForbidScheme       = "TestScheme";
            });
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>Returns a client that presents itself as an authenticated admin.</summary>
    public HttpClient CreateAdminClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", "admin");
        return client;
    }

    /// <summary>Returns a client with no authentication credentials.</summary>
    public HttpClient CreateAnonymousClient() => CreateClient();

    /// <summary>Seeds the in-memory database synchronously before the test runs.</summary>
    public void Seed(Action<AppDbContext> seed)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        seed(db);
        db.SaveChanges();
    }
}

/// <summary>
/// Test authentication handler.
/// Reads the X-Test-Role request header; if present it returns an authenticated
/// principal with that role, otherwise it returns NoResult (anonymous).
/// </summary>
public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-Role", out var roleValues))
            return Task.FromResult(AuthenticateResult.NoResult());

        var role = roleValues.FirstOrDefault() ?? "admin";
        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, "999"),
            new(ClaimTypes.Name, "TestUser"),
            new(ClaimTypes.Role, role)
        ];

        var identity  = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket    = new AuthenticationTicket(principal, "TestScheme");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
