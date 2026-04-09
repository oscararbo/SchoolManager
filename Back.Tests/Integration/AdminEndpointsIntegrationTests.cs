using Back.Api.Application.Dtos;
using Back.Api.Domain.Entities;
using Back.Api.Persistence.Context;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Back.Tests.Integration;

/// <summary>
/// Integration tests for the admin read-model endpoints.
/// Each test class gets its own WebAppFactory (isolated in-memory DB).
/// </summary>
public class AdminEndpointsIntegrationTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public AdminEndpointsIntegrationTests(WebAppFactory factory)
    {
        _factory = factory;
    }

    #region Auth guard

    [Theory]
    [InlineData("GET",  "/api/admin/matriculas")]
    [InlineData("GET",  "/api/admin/imparticiones")]
    [InlineData("GET",  "/api/admin/stats/cursos")]
    [InlineData("GET",  "/api/admin/stats/cursos/1")]
    public async Task AnonymousRequest_ReturnsUnauthorized(string method, string path)
    {
        var client = _factory.CreateAnonymousClient();
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region GET /api/admin/stats/cursos

    [Fact]
    public async Task GetCursosStatsSelector_ReturnsOk_WithSeededCourses()
    {
        _factory.Seed(db =>
        {
            db.Cursos.AddRange(
                new Curso { Nombre = "1 ESO" },
                new Curso { Nombre = "2 ESO" });
        });
        var client = _factory.CreateAdminClient();

        var response = await client.GetAsync("/api/admin/stats/cursos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<CursoStatsSelectorDto>>();
        Assert.NotNull(payload);
        Assert.True(payload.Count >= 2);
        Assert.All(payload, c => Assert.False(string.IsNullOrWhiteSpace(c.Curso)));
    }

    [Fact]
    public async Task GetCursosStatsSelector_ReturnsOk_WhenNoCourses()
    {
        var client = _factory.CreateAdminClient();

        var response = await client.GetAsync("/api/admin/stats/cursos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<CursoStatsSelectorDto>>();
        Assert.NotNull(payload);
    }

    #endregion

    #region GET /api/admin/stats/cursos/{cursoId}

    [Fact]
    public async Task GetStatsByCurso_ReturnsOk_WhenCourseExists()
    {
        Curso curso = new() { Nombre = "3 ESO Int" };
        _factory.Seed(db => db.Cursos.Add(curso));

        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync($"/api/admin/stats/cursos/{curso.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CursoNotasStatsResponseDto>();
        Assert.NotNull(payload);
        Assert.Equal("3 ESO Int", payload.Curso);
    }

    [Fact]
    public async Task GetStatsByCurso_ReturnsNotFound_WhenCourseDoesNotExist()
    {
        var client = _factory.CreateAdminClient();

        var response = await client.GetAsync("/api/admin/stats/cursos/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region POST /api/admin/stats/cursos/comparar

    [Fact]
    public async Task CompareCursos_ReturnsBadRequest_WhenFewerThanTwoValidCourses()
    {
        var client = _factory.CreateAdminClient();

        var response = await client.PostAsJsonAsync(
            "/api/admin/stats/cursos/comparar",
            new CompararCursosRequestDto { CursoIds = [0, -1] });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CompareCursos_ReturnsOk_WithTwoValidCourses()
    {
        Curso c1 = new() { Nombre = "1 BACH Comp" };
        Curso c2 = new() { Nombre = "2 BACH Comp" };
        _factory.Seed(db =>
        {
            db.Cursos.Add(c1);
            db.Cursos.Add(c2);
        });

        var client = _factory.CreateAdminClient();
        var response = await client.PostAsJsonAsync(
            "/api/admin/stats/cursos/comparar",
            new CompararCursosRequestDto { CursoIds = [c1.Id, c2.Id] });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ComparacionCursosResponseDto>();
        Assert.NotNull(payload);
        Assert.Equal(2, payload.Cursos.Count());
    }

    #endregion

    #region GET /api/admin/matriculas

    [Fact]
    public async Task GetMatriculas_ReturnsOk_WithSeededEnrollments()
    {
        Curso curso = new() { Nombre = "4 ESO Mat" };
        _factory.Seed(db =>
        {
            db.Cursos.Add(curso);
            db.SaveChanges();

            var asignatura = new Asignatura { Nombre = "Matematicas", CursoId = curso.Id };
            db.Asignaturas.Add(asignatura);
            db.SaveChanges();

            var estudiante = new Estudiante
            {
                Nombre = "Ana Test",
                Correo = $"ana_mat_{Guid.NewGuid():N}@test.com",
                Contrasena = "hash",
                CursoId = curso.Id
            };
            db.Estudiantes.Add(estudiante);
            db.SaveChanges();

            db.EstudianteAsignaturas.Add(new EstudianteAsignatura
            {
                EstudianteId = estudiante.Id,
                AsignaturaId = asignatura.Id
            });
        });

        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/api/admin/matriculas");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<AdminMatriculaListReadModelDto>>();
        Assert.NotNull(payload);

        var anaEntry = payload.FirstOrDefault(m => m.Estudiante == "Ana Test");
        Assert.NotNull(anaEntry);
        Assert.Equal("4 ESO Mat", anaEntry.Curso);
        Assert.Single(anaEntry.Asignaturas);
        Assert.Equal("Matematicas", anaEntry.Asignaturas.First().Asignatura);
    }

    [Fact]
    public async Task GetMatriculas_ReturnsOk_StudentWithNoEnrollments()
    {
        Curso curso = new() { Nombre = "1 ESO Vacio" };
        _factory.Seed(db =>
        {
            db.Cursos.Add(curso);
            db.SaveChanges();

            db.Estudiantes.Add(new Estudiante
            {
                Nombre = "Sin Asigs",
                Correo = $"sinasigs_{Guid.NewGuid():N}@test.com",
                Contrasena = "hash",
                CursoId = curso.Id
            });
        });

        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/api/admin/matriculas");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<AdminMatriculaListReadModelDto>>();
        Assert.NotNull(payload);

        var entry = payload.FirstOrDefault(m => m.Estudiante == "Sin Asigs");
        Assert.NotNull(entry);
        Assert.Empty(entry.Asignaturas);
    }

    #endregion

    #region GET /api/admin/imparticiones

    [Fact]
    public async Task GetImparticiones_ReturnsOk_WithSeededAssignments()
    {
        Curso curso = new() { Nombre = "2 ESO Imp" };
        _factory.Seed(db =>
        {
            db.Cursos.Add(curso);
            db.SaveChanges();

            var asignatura = new Asignatura { Nombre = "Lengua", CursoId = curso.Id };
            db.Asignaturas.Add(asignatura);
            db.SaveChanges();

            var profesor = new Profesor
            {
                Nombre = "Luis Test",
                Correo = $"luis_{Guid.NewGuid():N}@test.com",
                Contrasena = "hash"
            };
            db.Profesores.Add(profesor);
            db.SaveChanges();

            db.ProfesorAsignaturaCursos.Add(new ProfesorAsignaturaCurso
            {
                ProfesorId  = profesor.Id,
                AsignaturaId = asignatura.Id,
                CursoId     = curso.Id
            });
        });

        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/api/admin/imparticiones");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<AdminImparticionListReadModelDto>>();
        Assert.NotNull(payload);

        var entry = payload.FirstOrDefault(i =>
            i.Asignatura == "Lengua" && i.Curso == "2 ESO Imp");
        Assert.NotNull(entry);
        Assert.Equal("Luis Test", entry.Profesor);
    }

    [Fact]
    public async Task GetImparticiones_ReturnsOk_EmptyListWhenNoAssignments()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/api/admin/imparticiones");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<AdminImparticionListReadModelDto>>();
        Assert.NotNull(payload);
    }

    #endregion
}

