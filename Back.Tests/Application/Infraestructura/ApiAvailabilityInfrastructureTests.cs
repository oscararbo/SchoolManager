using System.Net;
using System.Net.Http.Headers;
using Back.Api.Application.Configuration;
using Back.Tests.Application.TestSupport.Auth;
using Xunit;

namespace Back.Tests.Application.Infraestructura;

public class ApiAvailabilityInfrastructureTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory webAppFactory;

    public ApiAvailabilityInfrastructureTests(TestWebAppFactory webAppFactory)
    {
        this.webAppFactory = webAppFactory;
    }

    [Fact]
    public async Task AdminEndpoint_WhenApiIsUp_ReturnsOk()
    {
        using var authenticatedClient = CreateAuthenticatedClient(Roles.Admin);
        var response = await authenticatedClient.GetAsync("/api/admin");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private HttpClient CreateAuthenticatedClient(string userRole)
    {
        var authenticatedClient = webAppFactory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestWebAppFactory.TestScheme);
        authenticatedClient.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, userRole);
        return authenticatedClient;
    }
}