using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Back.Api.Application.Configuration;
using Back.Tests.Application.TestSupport.Auth;
using Xunit;

namespace Back.Tests.Application.IntegrationTests;

public class AdminEndpointsIntegrationTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory webAppFactory;

    public AdminEndpointsIntegrationTests(TestWebAppFactory webAppFactory)
    {
        this.webAppFactory = webAppFactory;
    }

    [Fact]
    public async Task CreateCurso_ConRolAdmin_PersisteYDevuelve201()
    {
        using var authenticatedClient = CreateAuthenticatedClient(Roles.Admin);
        var cursoNombre = $"Curso Integracion {Guid.NewGuid():N}";

        var response = await authenticatedClient.PostAsJsonAsync("/api/cursos", new { Nombre = cursoNombre });
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains("/api/cursos/", response.Headers.Location?.ToString());
        Assert.Contains(cursoNombre, responseBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetCursos_DespuesDeCrear_DevuelveInformacionDelCurso()
    {
        using var authenticatedClient = CreateAuthenticatedClient(Roles.Admin);
        var cursoNombre = $"Curso Integracion {Guid.NewGuid():N}";

        var createResponse = await authenticatedClient.PostAsJsonAsync("/api/cursos", new { Nombre = cursoNombre });
        var response = await authenticatedClient.GetAsync("/api/cursos");
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(cursoNombre, responseBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetCursoById_DespuesDeCrear_DevuelveInformacionDelCurso()
    {
        using var authenticatedClient = CreateAuthenticatedClient(Roles.Admin);
        var cursoNombre = $"Curso Integracion {Guid.NewGuid():N}";

        var createResponse = await authenticatedClient.PostAsJsonAsync("/api/cursos", new { Nombre = cursoNombre });
        var createResponseBody = await createResponse.Content.ReadAsStringAsync();
        var cursoId = ReadIdFromCreatedCurso(createResponseBody);

        var response = await authenticatedClient.GetAsync($"/api/cursos/{cursoId}");
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(cursoNombre, responseBody, StringComparison.OrdinalIgnoreCase);
    }

    private HttpClient CreateAuthenticatedClient(string userRole)
    {
        var authenticatedClient = webAppFactory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestWebAppFactory.TestScheme);
        authenticatedClient.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, userRole);
        return authenticatedClient;
    }

    private static int ReadIdFromCreatedCurso(string responseBody)
    {
        using var jsonDocument = JsonDocument.Parse(responseBody);
        var root = jsonDocument.RootElement;

        if (root.TryGetProperty("id", out var idValue) && idValue.TryGetInt32(out var id))
        {
            return id;
        }

        if (root.TryGetProperty("Id", out var pascalIdValue) && pascalIdValue.TryGetInt32(out var pascalId))
        {
            return pascalId;
        }

        throw new InvalidOperationException("No se encontro el campo id en la respuesta de creacion de curso.");
    }
}