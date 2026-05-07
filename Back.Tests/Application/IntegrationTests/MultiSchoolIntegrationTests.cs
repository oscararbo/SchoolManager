using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Back.Api.Application.Abstractions.Security;
using Back.Api.Application.Configuration;
using Back.Api.Domain.Entities;
using Back.Api.Persistence.Context;
using Back.Tests.Application.TestSupport.Auth;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Back.Tests.Application.IntegrationTests;

public class MultiSchoolIntegrationTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory webAppFactory;

    public MultiSchoolIntegrationTests(TestWebAppFactory webAppFactory)
    {
        this.webAppFactory = webAppFactory;
    }

    [Fact]
    public async Task GetCursos_AdminSoloVeCursosDeSuColegio()
    {
        await using var scope = webAppFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Colegios.AddRange(
            new Colegio { Id = 101, Nombre = "Colegio Uno", Slug = "colegio-uno" },
            new Colegio { Id = 102, Nombre = "Colegio Dos", Slug = "colegio-dos" });
        db.Cursos.AddRange(
            new Curso { Nombre = "Curso Colegio Uno", ColegioId = 101 },
            new Curso { Nombre = "Curso Colegio Dos", ColegioId = 102 });
        await db.SaveChangesAsync();

        using var clientSchoolOne = CreateAuthenticatedClient(Roles.Admin, 101, "colegio-uno");
        var response = await clientSchoolOne.GetAsync("/api/cursos");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Curso Colegio Uno", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Curso Colegio Dos", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_MismoCorreoEnDistintosColegios_ResuelvePorSlug()
    {
        await using var scope = webAppFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

        var schoolOne = new Colegio { Nombre = "Colegio A", Slug = "colegio-a" };
        var schoolTwo = new Colegio { Nombre = "Colegio B", Slug = "colegio-b" };
        db.Colegios.AddRange(schoolOne, schoolTwo);
        await db.SaveChangesAsync();

        db.Admins.AddRange(
            new Admin
            {
                Nombre = "Admin A",
                Cuenta = new Cuenta
                {
                    Correo = "admin@demo.com",
                    Contrasena = passwordService.Hash("ClaveA1!"),
                    Rol = Roles.Admin,
                    ColegioId = schoolOne.Id
                }
            },
            new Admin
            {
                Nombre = "Admin B",
                Cuenta = new Cuenta
                {
                    Correo = "admin@demo.com",
                    Contrasena = passwordService.Hash("ClaveB1!"),
                    Rol = Roles.Admin,
                    ColegioId = schoolTwo.Id
                }
            });
        await db.SaveChangesAsync();

        using var schoolAClient = webAppFactory.CreateClient();
        schoolAClient.DefaultRequestHeaders.Add("X-School-Slug", "colegio-a");
        var schoolAResponse = await schoolAClient.PostAsJsonAsync("/api/auth/login", new { correo = "admin@demo.com", contrasena = "ClaveA1!" });
        var schoolABody = await schoolAResponse.Content.ReadAsStringAsync();

        using var schoolBClient = webAppFactory.CreateClient();
        schoolBClient.DefaultRequestHeaders.Add("X-School-Slug", "colegio-b");
        var schoolBResponse = await schoolBClient.PostAsJsonAsync("/api/auth/login", new { correo = "admin@demo.com", contrasena = "ClaveB1!" });
        var schoolBBody = await schoolBResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, schoolAResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, schoolBResponse.StatusCode);
        Assert.Contains("colegio-a", schoolABody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("colegio-b", schoolBBody, StringComparison.OrdinalIgnoreCase);
    }

    private HttpClient CreateAuthenticatedClient(string userRole, int schoolId, string schoolSlug)
    {
        var authenticatedClient = webAppFactory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestWebAppFactory.TestScheme);
        authenticatedClient.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, userRole);
        authenticatedClient.DefaultRequestHeaders.Add(TestAuthHandler.SchoolIdHeader, schoolId.ToString());
        authenticatedClient.DefaultRequestHeaders.Add(TestAuthHandler.SchoolSlugHeader, schoolSlug);
        return authenticatedClient;
    }
}