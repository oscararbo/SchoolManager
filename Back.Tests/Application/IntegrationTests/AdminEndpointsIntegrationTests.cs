using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
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

    [Fact]
    public async Task CrearHorarioSolapadoEnMismoCurso_Devuelve400()
    {
        using var authenticatedClient = CreateAuthenticatedClient(Roles.Admin);

        var courseName = $"Curso Horarios {Guid.NewGuid():N}";
        var createCourseResponse = await authenticatedClient.PostAsJsonAsync("/api/cursos", new { Nombre = courseName });
        var courseId = ReadIdFromCreatedCurso(await createCourseResponse.Content.ReadAsStringAsync());

        var subjectAName = $"Asignatura A {Guid.NewGuid():N}";
        var subjectBName = $"Asignatura B {Guid.NewGuid():N}";

        var createSubjectAResponse = await authenticatedClient.PostAsJsonAsync("/api/asignaturas", new { Nombre = subjectAName, CursoId = courseId });
        var createSubjectBResponse = await authenticatedClient.PostAsJsonAsync("/api/asignaturas", new { Nombre = subjectBName, CursoId = courseId });

        var subjectAId = ReadIdFromCreatedCurso(await createSubjectAResponse.Content.ReadAsStringAsync());
        var subjectBId = ReadIdFromCreatedCurso(await createSubjectBResponse.Content.ReadAsStringAsync());

        var createHorarioResponse = await authenticatedClient.PostAsJsonAsync("/api/admin/horarios", new
        {
            AsignaturaId = subjectAId,
            DiaSemana = 1,
            HoraInicio = "08:30",
            HoraFin = "09:25",
            Aula = "Aula A"
        });

        var overlapResponse = await authenticatedClient.PostAsJsonAsync("/api/admin/horarios", new
        {
            AsignaturaId = subjectBId,
            DiaSemana = 1,
            HoraInicio = "09:00",
            HoraFin = "09:40",
            Aula = "Aula B"
        });
        var overlapBody = await overlapResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, createCourseResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, createSubjectAResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, createSubjectBResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, createHorarioResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, overlapResponse.StatusCode);
        Assert.Contains("Ese curso ya tiene otra asignatura en ese tramo horario", overlapBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ImportarHorariosCsv_ConDatosValidos_CreaHorarios()
    {
        using var authenticatedClient = CreateAuthenticatedClient(Roles.Admin);

        var courseName = $"Curso CSV Horarios {Guid.NewGuid():N}";
        var createCourseResponse = await authenticatedClient.PostAsJsonAsync("/api/cursos", new { Nombre = courseName });
        var courseId = ReadIdFromCreatedCurso(await createCourseResponse.Content.ReadAsStringAsync());

        var subjectName = $"Asignatura CSV Horarios {Guid.NewGuid():N}";
        var createSubjectResponse = await authenticatedClient.PostAsJsonAsync("/api/asignaturas", new { Nombre = subjectName, CursoId = courseId });

        var csv = $"asignaturaNombre,cursoNombre,diaSemana,horaInicio,horaFin,aula\n{subjectName},{courseName},1,10:30,11:25,Aula CSV";
        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        form.Add(fileContent, "file", "horarios.csv");

        var importResponse = await authenticatedClient.PostAsync("/api/admin/csv/horarios", form);
        var importBody = await importResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, createCourseResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, createSubjectResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);
        Assert.Equal(1, ReadNumericProperty(importBody, "creados", "Creados"));
    }

    [Fact]
    public async Task ImportarHorariosCsv_ConSolapeEnMismoCurso_Devuelve400()
    {
        using var authenticatedClient = CreateAuthenticatedClient(Roles.Admin);

        var courseName = $"Curso CSV Solape {Guid.NewGuid():N}";
        var createCourseResponse = await authenticatedClient.PostAsJsonAsync("/api/cursos", new { Nombre = courseName });
        var courseId = ReadIdFromCreatedCurso(await createCourseResponse.Content.ReadAsStringAsync());

        var subjectAName = $"Asignatura CSV A {Guid.NewGuid():N}";
        var subjectBName = $"Asignatura CSV B {Guid.NewGuid():N}";
        var createSubjectAResponse = await authenticatedClient.PostAsJsonAsync("/api/asignaturas", new { Nombre = subjectAName, CursoId = courseId });
        var createSubjectBResponse = await authenticatedClient.PostAsJsonAsync("/api/asignaturas", new { Nombre = subjectBName, CursoId = courseId });

        var csv = string.Join('\n',
            "asignaturaNombre,cursoNombre,diaSemana,horaInicio,horaFin,aula",
            $"{subjectAName},{courseName},1,08:30,09:25,Aula A",
            $"{subjectBName},{courseName},1,09:00,09:40,Aula B");

        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        form.Add(fileContent, "file", "horarios-solape.csv");

        var importResponse = await authenticatedClient.PostAsync("/api/admin/csv/horarios", form);
        var importBody = await importResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, createCourseResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, createSubjectAResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, createSubjectBResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, importResponse.StatusCode);
        Assert.Contains("solapado", importBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ImportarHorariosCsv_ConDiaSemanaInvalido_Devuelve400()
    {
        using var authenticatedClient = CreateAuthenticatedClient(Roles.Admin);

        var courseName = $"Curso CSV Dia {Guid.NewGuid():N}";
        var createCourseResponse = await authenticatedClient.PostAsJsonAsync("/api/cursos", new { Nombre = courseName });
        var courseId = ReadIdFromCreatedCurso(await createCourseResponse.Content.ReadAsStringAsync());

        var subjectName = $"Asignatura CSV Dia {Guid.NewGuid():N}";
        var createSubjectResponse = await authenticatedClient.PostAsJsonAsync("/api/asignaturas", new { Nombre = subjectName, CursoId = courseId });

        var csv = $"asignaturaNombre,cursoNombre,diaSemana,horaInicio,horaFin,aula\n{subjectName},{courseName},9,10:30,11:25,Aula CSV";
        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        form.Add(fileContent, "file", "horarios-dia-invalido.csv");

        var importResponse = await authenticatedClient.PostAsync("/api/admin/csv/horarios", form);
        var importBody = await importResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, createCourseResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, createSubjectResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, importResponse.StatusCode);
        Assert.Contains("diaSemana no valido", importBody, StringComparison.OrdinalIgnoreCase);
    }

    private HttpClient CreateAuthenticatedClient(string userRole)
    {
        var authenticatedClient = webAppFactory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestWebAppFactory.TestScheme);
        authenticatedClient.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, userRole);
        authenticatedClient.DefaultRequestHeaders.Add(TestAuthHandler.SchoolIdHeader, "1");
        return authenticatedClient;
    }

    private static int ReadIdFromCreatedCurso(string responseBody)
        => ReadNumericProperty(responseBody, "id", "Id");

    private static int ReadNumericProperty(string responseBody, params string[] candidates)
    {
        using var jsonDocument = JsonDocument.Parse(responseBody);
        var root = jsonDocument.RootElement;

        foreach (var candidate in candidates)
        {
            if (root.TryGetProperty(candidate, out var value) && value.TryGetInt32(out var parsed))
            {
                return parsed;
            }
        }

        throw new InvalidOperationException($"No se encontro ninguno de los campos numericos esperados: {string.Join(", ", candidates)}.");
    }
}